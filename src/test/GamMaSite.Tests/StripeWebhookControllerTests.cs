using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;
using System.Threading.Tasks;
using GamMaSite.Controllers;
using GamMaSite.Data;
using GamMaSite.Models;
using GamMaSite.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Stripe;
using Xunit;

namespace GamMaSite.Tests;

public sealed class StripeWebhookControllerTests
{
    private const string WebhookSecret = "whsec_test_secret";
    private const string MembershipProductId = "prod_membership";

    [Fact]
    public async Task Receive_RejectsInvalidSignature()
    {
        await using var db = CreateDb();
        var controller = CreateController(db, new Mock<IStripeService>().Object);
        SetRequest(controller, "{}", "t=1,v1=invalid");

        var result = await controller.Receive(default);

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task Receive_MarksOneTimeMembershipPaymentAsPaid()
    {
        await using var db = CreateDb();
        db.Users.Add(new SiteUser { Id = "user-1", UserName = "member@example.com", Status = UserStatus.OPRETTET });
        await db.SaveChangesAsync();

        var controller = CreateController(db, new Mock<IStripeService>().Object);
        var created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = $"{{\"id\":\"evt_one_time\",\"object\":\"event\",\"created\":{created},\"data\":{{\"object\":{{\"id\":\"cs_one_time\",\"object\":\"checkout.session\",\"mode\":\"payment\",\"payment_status\":\"paid\",\"client_reference_id\":\"user-1\",\"created\":{created},\"metadata\":{{\"Product\":\"{MembershipProductId}\"}}}}}},\"livemode\":false,\"type\":\"checkout.session.completed\"}}";
        SetRequest(controller, payload, Sign(payload));

        var result = await controller.Receive(default);

        Assert.IsType<OkResult>(result);
        var user = await db.Users.SingleAsync(item => item.Id == "user-1");
        Assert.Equal(UserStatus.BETALT, user.Status);
        Assert.NotEqual(default, user.KontingentDato);
    }

    [Fact]
    public async Task Receive_MarksPaymentOfferMembershipPaymentAsPaid()
    {
        await using var db = CreateDb();
        db.Users.Add(new SiteUser { Id = "user-1", UserName = "member@example.com", Status = UserStatus.OPRETTET });
        db.PaymentOffers.Add(new PaymentOffer
        {
            Scope = PaymentOfferScopes.Membership,
            Provider = PaymentOfferProviders.Stripe,
            OfferType = PaymentOfferTypes.OneTime,
            LinkLabel = "Betal én gang",
            ProductName = "Kontingent",
            AmountMinor = 15000,
            Currency = "DKK",
            IsActive = true,
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var offer = await db.PaymentOffers.SingleAsync();

        var controller = CreateController(db, new Mock<IStripeService>().Object);
        var created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = $"{{\"id\":\"evt_offer_membership\",\"object\":\"event\",\"created\":{created},\"data\":{{\"object\":{{\"id\":\"cs_offer_membership\",\"object\":\"checkout.session\",\"mode\":\"payment\",\"payment_status\":\"paid\",\"client_reference_id\":\"user-1\",\"created\":{created},\"metadata\":{{\"PaymentOfferId\":\"{offer.Id}\"}}}}}},\"livemode\":false,\"type\":\"checkout.session.completed\"}}";
        SetRequest(controller, payload, Sign(payload));

        var result = await controller.Receive(default);

        Assert.IsType<OkResult>(result);
        var user = await db.Users.SingleAsync(item => item.Id == "user-1");
        Assert.Equal(UserStatus.BETALT, user.Status);
        Assert.NotEqual(default, user.KontingentDato);
    }

    [Fact]
    public async Task Receive_MarksEventRegistrationAsPaid()
    {
        await using var db = CreateDb();
        db.Users.Add(new SiteUser { Id = "user-1", UserName = "member@example.com", Status = UserStatus.OPRETTET });
        var eventItem = new ContentItem
        {
            Type = ContentTypes.Event,
            Status = ContentStatuses.Published,
            Title = "Test event",
            Slug = "test-event",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(1).AddHours(2),
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        };
        var offer = new PaymentOffer
        {
            ContentItem = eventItem,
            Scope = PaymentOfferScopes.Event,
            Provider = PaymentOfferProviders.Stripe,
            OfferType = PaymentOfferTypes.OneTime,
            ProductName = "Test ticket",
            IsActive = true,
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        };
        db.PaymentOffers.Add(offer);
        db.EventRegistrations.Add(new EventRegistration
        {
            ContentItem = eventItem,
            UserId = "user-1",
            RegistrationType = RegistrationTypes.Attendee,
            Registered = false,
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var controller = CreateController(db, new Mock<IStripeService>().Object);
        var created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = $"{{\"id\":\"evt_event_payment\",\"object\":\"event\",\"created\":{created},\"data\":{{\"object\":{{\"id\":\"cs_event_payment\",\"object\":\"checkout.session\",\"mode\":\"payment\",\"payment_status\":\"paid\",\"client_reference_id\":\"user-1\",\"created\":{created},\"metadata\":{{\"PaymentOfferId\":\"{offer.Id}\"}}}}}},\"livemode\":false,\"type\":\"checkout.session.completed\"}}";
        SetRequest(controller, payload, Sign(payload));

        var result = await controller.Receive(default);

        Assert.IsType<OkResult>(result);
        var registration = await db.EventRegistrations.SingleAsync();
        Assert.True(registration.Registered);
    }

    [Fact]
    public async Task Receive_UpsertsRecurringMembershipSubscription()
    {
        await using var db = CreateDb();
        db.Users.Add(new SiteUser { Id = "user-1", UserName = "member@example.com", Status = UserStatus.OPRETTET });
        await db.SaveChangesAsync();

        var stripe = new Mock<IStripeService>();
        var periodStart = DateTime.UtcNow;
        var periodEnd = periodStart.AddYears(1);
        var subscription = new Subscription
        {
            Id = "sub_test",
            Created = periodStart,
            Status = "active",
            CancelAtPeriodEnd = false,
            Metadata = new Dictionary<string, string>
            {
                ["Product"] = MembershipProductId,
                ["ProductName"] = "Kontingent"
            },
            Items = new StripeList<SubscriptionItem>
            {
                Data = new List<SubscriptionItem>
                {
                    new()
                    {
                        Price = new Price
                        {
                            Id = "price_recurring",
                            ProductId = MembershipProductId
                        },
                        CurrentPeriodStart = periodStart,
                        CurrentPeriodEnd = periodEnd
                    }
                }
            }
        };
        stripe.Setup(value => value.GetSubscriptionAsync("sub_test")).ReturnsAsync(subscription);
        stripe.Setup(value => value.GetProductAsync(MembershipProductId))
            .ReturnsAsync(new Product { Id = MembershipProductId, Name = "Kontingent" });

        var controller = CreateController(db, stripe.Object);
        var created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = $"{{\"id\":\"evt_subscription\",\"object\":\"event\",\"created\":{created},\"data\":{{\"object\":{{\"id\":\"cs_subscription\",\"object\":\"checkout.session\",\"mode\":\"subscription\",\"payment_status\":\"paid\",\"subscription\":\"sub_test\",\"client_reference_id\":\"user-1\",\"created\":{created},\"metadata\":{{\"Product\":\"{MembershipProductId}\"}}}}}},\"livemode\":false,\"type\":\"checkout.session.completed\"}}";
        SetRequest(controller, payload, Sign(payload));

        var result = await controller.Receive(default);

        Assert.IsType<OkResult>(result);
        var record = await db.PaymentSubscriptions.SingleAsync();
        Assert.Equal("user-1", record.UserId);
        Assert.Equal("Stripe", record.Provider);
        Assert.Equal("sub_test", record.ProviderSubscriptionId);
        Assert.Equal("Kontingent", record.ProductName);
        Assert.Equal("price_recurring", record.ProviderPriceId);
        Assert.Equal("active", record.Status);
        Assert.Equal(periodEnd, record.NextChargeAt);

        var user = await db.Users.SingleAsync(item => item.Id == "user-1");
        Assert.Equal(UserStatus.BETALT, user.Status);
        Assert.NotEqual(default, user.KontingentDato);
    }

    private static StripeWebhookController CreateController(ApplicationDbContext db, IStripeService stripe)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["StripeConfig:WebhookSecret"] = WebhookSecret,
                ["StripeConfig:MembershipProductId"] = MembershipProductId
            })
            .Build();

        return new StripeWebhookController(db, stripe, configuration, NullLogger<StripeWebhookController>.Instance);
    }

    private static void SetRequest(StripeWebhookController controller, string payload, string signature)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        httpContext.Request.Headers["Stripe-Signature"] = signature;
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    private static string Sign(string payload)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signedPayload = $"{timestamp}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(WebhookSecret));
        var signature = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload))).ToLowerInvariant();
        return $"t={timestamp},v1={signature}";
    }

    private static ApplicationDbContext CreateDb() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);
}
