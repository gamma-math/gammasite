using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
        db.PaymentOffers.Add(new PaymentOffer
        {
            Scope = PaymentOfferScopes.Membership,
            Provider = PaymentOfferProviders.Stripe,
            OfferType = PaymentOfferTypes.OneTime,
            ProductName = "Kontingent",
            ProviderProductId = MembershipProductId,
            IsActive = true,
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        });
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

    [Theory]
    [InlineData(RegistrationTypes.Attendee)]
    [InlineData(RegistrationTypes.Organizer)]
    public async Task Receive_MarksEventRegistrationAsPaid(string registrationType)
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
            RegistrationType = registrationType,
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
    public async Task Receive_MarksEventRegistrationAsPaidWhenOfferWasDeletedAfterCheckout()
    {
        await using var db = CreateDb();
        db.Users.Add(new SiteUser { Id = "user-deleted-offer", UserName = "member@example.com", Status = UserStatus.OPRETTET });
        var eventItem = new ContentItem
        {
            Type = ContentTypes.Event,
            Status = ContentStatuses.Published,
            Title = "Deleted offer event",
            Slug = "deleted-offer-event",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(1).AddHours(2),
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        };
        db.ContentItems.Add(eventItem);
        db.EventRegistrations.Add(new EventRegistration
        {
            ContentItem = eventItem,
            UserId = "user-deleted-offer",
            RegistrationType = RegistrationTypes.Organizer,
            Registered = false,
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var controller = CreateController(db, new Mock<IStripeService>().Object);
        var created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = JsonSerializer.Serialize(new
        {
            id = "evt_deleted_offer",
            @object = "event",
            created,
            data = new
            {
                @object = new
                {
                    id = "cs_deleted_offer",
                    @object = "checkout.session",
                    mode = "payment",
                    payment_status = "paid",
                    client_reference_id = "user-deleted-offer",
                    created,
                    metadata = new Dictionary<string, string>
                    {
                        ["PaymentOfferId"] = "987654",
                        ["ContentItemId"] = eventItem.Id.ToString()
                    }
                }
            },
            livemode = false,
            type = "checkout.session.completed"
        });
        SetRequest(controller, payload, Sign(payload));

        var result = await controller.Receive(default);

        Assert.IsType<OkResult>(result);
        Assert.True((await db.EventRegistrations.SingleAsync()).Registered);
    }

    [Fact]
    public async Task Receive_UpsertsRecurringMembershipSubscription()
    {
        await using var db = CreateDb();
        db.Users.Add(new SiteUser { Id = "user-1", UserName = "member@example.com", Status = UserStatus.OPRETTET });
        db.PaymentOffers.Add(new PaymentOffer
        {
            Scope = PaymentOfferScopes.Membership,
            Provider = PaymentOfferProviders.Stripe,
            OfferType = PaymentOfferTypes.Recurring,
            ProductName = "Kontingent",
            ProviderProductId = MembershipProductId,
            ProviderPriceId = "price_recurring",
            IsActive = true,
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        });
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
        Assert.Equal(PaymentOfferProviders.Stripe, record.Provider);
        Assert.Equal("sub_test", record.ProviderSubscriptionId);
        Assert.Equal("Kontingent", record.ProductName);
        Assert.Equal("price_recurring", record.ProviderPriceId);
        Assert.Equal("active", record.Status);
        Assert.Equal(periodEnd, record.NextChargeAt);

        var user = await db.Users.SingleAsync(item => item.Id == "user-1");
        Assert.Equal(UserStatus.BETALT, user.Status);
        Assert.NotEqual(default, user.KontingentDato);

        SetRequest(controller, payload, Sign(payload));
        var duplicateResult = await controller.Receive(default);

        Assert.IsType<OkResult>(duplicateResult);
        Assert.Single(await db.PaymentSubscriptions.ToListAsync());
    }

    [Theory]
    [InlineData("invoice.paid")]
    [InlineData("invoice.payment_succeeded")]
    public async Task Receive_SendsRecurringPaymentEmailForSuccessfulSubscriptionInvoice(string eventType)
    {
        await using var db = CreateDb();
        var previousPaymentAt = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var paymentAt = previousPaymentAt.AddMonths(1);
        db.Users.Add(new SiteUser
        {
            Id = "user-recurring-email",
            UserName = "member@example.com",
            Email = "member@example.com",
            Navn = "Member",
            Status = UserStatus.BETALT
        });
        db.PaymentOffers.Add(new PaymentOffer
        {
            Scope = PaymentOfferScopes.Membership,
            Provider = PaymentOfferProviders.Stripe,
            OfferType = PaymentOfferTypes.Recurring,
            ProductName = "Kontingent",
            AmountMinor = 15000,
            Currency = "DKK",
            BillingInterval = "YEAR",
            BillingIntervalCount = 1,
            ProviderProductId = MembershipProductId,
            ProviderPriceId = "price_recurring",
            IsActive = true,
            Created = previousPaymentAt,
            Updated = previousPaymentAt
        });
        db.PaymentSubscriptions.Add(new PaymentSubscription
        {
            UserId = "user-recurring-email",
            Provider = PaymentOfferProviders.Stripe,
            ProviderSubscriptionId = "sub_recurring_email",
            Status = "active",
            LastPaidAt = previousPaymentAt,
            Created = previousPaymentAt,
            Updated = previousPaymentAt
        });
        await db.SaveChangesAsync();
        Assert.Equal(previousPaymentAt, (await db.PaymentSubscriptions.SingleAsync()).LastPaidAt);

        var stripe = new Mock<IStripeService>();
        stripe.Setup(value => value.GetSubscriptionAsync("sub_recurring_email"))
            .ReturnsAsync(new Subscription
            {
                Id = "sub_recurring_email",
                Created = previousPaymentAt,
                Status = "active",
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
                            Price = new Price { Id = "price_recurring", ProductId = MembershipProductId },
                            CurrentPeriodStart = paymentAt,
                            CurrentPeriodEnd = paymentAt.AddYears(1)
                        }
                    }
                }
            });
        stripe.Setup(value => value.GetProductAsync(MembershipProductId))
            .ReturnsAsync(new Product { Id = MembershipProductId, Name = "Kontingent" });
        var email = new Mock<ISystemEmailTemplateService>();
        var controller = CreateController(db, stripe.Object, email.Object);
        var created = new DateTimeOffset(paymentAt).ToUnixTimeSeconds();
        var payload = $"{{\"id\":\"evt_recurring_payment_{eventType.Replace('.', '_')}\",\"object\":\"event\",\"created\":{created},\"data\":{{\"object\":{{\"id\":\"in_recurring_payment\",\"object\":\"invoice\",\"created\":{created},\"parent\":{{\"subscription_details\":{{\"subscription\":\"sub_recurring_email\"}}}}}}}},\"livemode\":false,\"type\":\"{eventType}\"}}";
        SetRequest(controller, payload, Sign(payload));

        var result = await controller.Receive(default);

        Assert.IsType<OkResult>(result);
        var updatedSubscription = await db.PaymentSubscriptions.SingleAsync();
        Assert.Equal("user-recurring-email", updatedSubscription.UserId);
        Assert.Equal(paymentAt, updatedSubscription.LastPaidAt);
        email.Verify(value => value.SendPaymentConfirmationAsync(
            "member@example.com",
            "Member",
            "Kontingent",
            15000,
            "DKK",
            PaymentOfferProviders.Stripe,
            paymentAt,
            true,
            "YEAR",
            1,
            null), Times.Once);
    }

    [Fact]
    public async Task Receive_IgnoresOlderSubscriptionWebhook()
    {
        await using var db = CreateDb();
        db.PaymentOffers.Add(new PaymentOffer
        {
            Scope = PaymentOfferScopes.Membership,
            Provider = PaymentOfferProviders.Stripe,
            OfferType = PaymentOfferTypes.Recurring,
            ProductName = "Kontingent",
            ProviderProductId = MembershipProductId,
            IsActive = true,
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        });
        db.PaymentSubscriptions.Add(new PaymentSubscription
        {
            UserId = "user-order-test",
            Provider = PaymentOfferProviders.Stripe,
            ProviderSubscriptionId = "sub_order",
            Status = "canceled",
            NextChargeAt = null,
            LastProcessedWebhookId = "newer-webhook",
            LastProcessedWebhookAt = new DateTime(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc),
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var controller = CreateController(db, new Mock<IStripeService>().Object);
        const string payload = "{\"id\":\"evt_older\",\"object\":\"event\",\"created\":1792668600,\"data\":{\"object\":{\"id\":\"sub_order\",\"object\":\"subscription\",\"status\":\"active\",\"metadata\":{\"Product\":\"prod_membership\"}}},\"livemode\":false,\"type\":\"customer.subscription.updated\"}";
        SetRequest(controller, payload, Sign(payload));

        var result = await controller.Receive(default);

        Assert.IsType<OkResult>(result);
        var subscription = await db.PaymentSubscriptions.SingleAsync();
        Assert.Equal("canceled", subscription.Status);
        Assert.Null(subscription.NextChargeAt);
        Assert.Equal("newer-webhook", subscription.LastProcessedWebhookId);
    }

    private static StripeWebhookController CreateController(
        ApplicationDbContext db,
        IStripeService stripe,
        ISystemEmailTemplateService systemEmailTemplateService = null!)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["StripeConfig:WebhookSecret"] = WebhookSecret,
            })
            .Build();

        return new StripeWebhookController(
            db,
            stripe,
            configuration,
            NullLogger<StripeWebhookController>.Instance,
            systemEmailTemplateService);
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
