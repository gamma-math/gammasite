using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GamMaSite.Controllers;
using GamMaSite.Data;
using GamMaSite.Models;
using GamMaSite.Services;
using GamMaSite.ViewModels.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Stripe.Checkout;
using Xunit;

namespace GamMaSite.Tests;

public sealed class ApiPaymentOffersControllerTests
{
    [Fact]
    public async Task Delete_RejectsUserWithoutEventEditAccess()
    {
        await using var db = CreateDatabase();
        var offer = await SeedOfferAsync(db);
        var access = new Mock<IAccessControlService>();
        access.Setup(value => value.CanEditEventAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>(), offer.ContentItemId.Value))
            .ReturnsAsync(false);

        var stripe = new Mock<IStripeService>();
        var controller = new ApiPaymentOffersController(db, stripe.Object, access.Object);

        var result = await controller.Delete(offer.Id);

        Assert.IsType<ForbidResult>(result);
        Assert.NotNull(await db.PaymentOffers.FindAsync(offer.Id));
        stripe.Verify(value => value.DeactivatePriceAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Delete_RemovesOfferAndLegacyPaymentLinkButPreservesManualPaymentLink()
    {
        await using var db = CreateDatabase();
        var offer = await SeedOfferAsync(db);
        var access = new Mock<IAccessControlService>();
        access.Setup(value => value.CanEditEventAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>(), offer.ContentItemId.Value))
            .ReturnsAsync(true);

        var stripe = new Mock<IStripeService>();
        stripe.Setup(value => value.DeactivatePriceAsync("price_event"))
            .ReturnsAsync(true);
        var controller = new ApiPaymentOffersController(db, stripe.Object, access.Object);

        var result = await controller.Delete(offer.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Null(await db.PaymentOffers.FindAsync(offer.Id));
        var remainingLinks = await db.ContentLinks
            .Where(link => link.ContentItemId == offer.ContentItemId)
            .ToListAsync();
        var remainingLink = Assert.Single(remainingLinks);
        Assert.Equal("https://tickets.example.com/event", remainingLink.Url);
        Assert.Equal("PAYMENT", remainingLink.Type);
        stripe.Verify(value => value.DeactivatePriceAsync("price_event"), Times.Once);
    }

    [Fact]
    public async Task SaveForEventStoresPaymentOfferLabelWithoutCreatingContentLink()
    {
        await using var db = CreateDatabase();
        var item = new ContentItem
        {
            Type = ContentTypes.Event,
            Status = ContentStatuses.Published,
            Title = "Test event",
            Slug = "test-event",
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow,
            Links =
            {
                new ContentLink
                {
                    Label = "Ekstern betaling",
                    Url = "https://tickets.example.com/event",
                    Type = "PAYMENT",
                    Created = DateTime.UtcNow,
                    Updated = DateTime.UtcNow
                }
            }
        };
        db.ContentItems.Add(item);
        await db.SaveChangesAsync();

        var access = new Mock<IAccessControlService>();
        access.Setup(value => value.CanEditEventAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>(), item.Id))
            .ReturnsAsync(true);
        var stripe = new Mock<IStripeService>();
        var controller = new ApiPaymentOffersController(db, stripe.Object, access.Object);

        var result = await controller.SaveForEvent(item.Id, new SavePaymentOfferRequest
        {
            LinkLabel = "Køb billet",
            Provider = PaymentOfferProviders.MobilePay,
            Amount = 125,
            ProductName = "Billet"
        });

        var response = Assert.IsType<OkObjectResult>(result);
        var offer = Assert.IsType<PaymentOfferDto>(response.Value);
        Assert.Equal("Køb billet", offer.LinkLabel);
        Assert.Equal("/react/events/test-event/pay/1", offer.PaymentUrl);
        Assert.Single(await db.PaymentOffers.Where(value => value.ContentItemId == item.Id).ToListAsync());
        var links = await db.ContentLinks.Where(link => link.ContentItemId == item.Id).ToListAsync();
        var manualLink = Assert.Single(links);
        Assert.Equal("https://tickets.example.com/event", manualLink.Url);
    }

    [Fact]
    public async Task GetForAdminReturnsEventAndMembershipOffers()
    {
        await using var db = CreateDatabase();
        var eventItem = new ContentItem
        {
            Type = ContentTypes.Event,
            Status = ContentStatuses.Published,
            Title = "Test event",
            Slug = "test-event",
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        };
        db.PaymentOffers.AddRange(
            new PaymentOffer
            {
                ContentItem = eventItem,
                Scope = PaymentOfferScopes.Event,
                Provider = PaymentOfferProviders.Stripe,
                OfferType = PaymentOfferTypes.OneTime,
                LinkLabel = "Køb billet",
                ProductName = "Billet",
                AmountMinor = 2500,
                Currency = "DKK",
                IsActive = true,
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow
            },
            new PaymentOffer
            {
                Scope = PaymentOfferScopes.Membership,
                Provider = PaymentOfferProviders.MobilePay,
                OfferType = PaymentOfferTypes.Recurring,
                LinkLabel = "Betal automatisk",
                ProductName = "Kontingent",
                AmountMinor = 15000,
                Currency = "DKK",
                IsActive = true,
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        var controller = new ApiPaymentOffersController(
            db,
            new Mock<IStripeService>().Object,
            new Mock<IAccessControlService>().Object);

        var result = Assert.IsType<OkObjectResult>(await controller.GetForAdmin());
        var offers = Assert.IsAssignableFrom<System.Collections.IEnumerable>(result.Value)
            .Cast<PaymentOfferDto>()
            .ToList();

        Assert.Equal(2, offers.Count);
        Assert.Contains(offers, offer => offer.Scope == PaymentOfferScopes.Event && offer.EventTitle == "Test event");
        Assert.Contains(offers, offer => offer.Scope == PaymentOfferScopes.Membership && offer.EventTitle == null);
    }

    [Fact]
    public async Task UpdateForAdminUpdatesProductSettings()
    {
        await using var db = CreateDatabase();
        var offer = new PaymentOffer
        {
            Scope = PaymentOfferScopes.Membership,
            Provider = PaymentOfferProviders.MobilePay,
            OfferType = PaymentOfferTypes.Recurring,
            LinkLabel = "Gammelt label",
            ProductName = "Kontingent",
            AmountMinor = 15000,
            Currency = "DKK",
            BillingInterval = "YEAR",
            BillingIntervalCount = 1,
            IsActive = true,
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        };
        db.PaymentOffers.Add(offer);
        await db.SaveChangesAsync();

        var controller = new ApiPaymentOffersController(
            db,
            new Mock<IStripeService>().Object,
            new Mock<IAccessControlService>().Object);

        var result = Assert.IsType<OkObjectResult>(await controller.UpdateForAdmin(offer.Id, new UpdatePaymentOfferRequest
        {
            LinkLabel = "Nyt label",
            ProductName = "Nyt kontingent",
            Amount = 175,
            BillingInterval = "YEAR",
            BillingIntervalCount = 1,
            IsActive = false
        }));

        var updated = Assert.IsType<PaymentOfferDto>(result.Value);
        Assert.Equal("Nyt label", updated.LinkLabel);
        Assert.Equal("Nyt kontingent", updated.ProductName);
        Assert.Equal(17500, updated.AmountMinor);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task CheckoutCreatesStripeReferencesForMembershipOffer()
    {
        await using var db = CreateDatabase();
        db.Users.Add(new SiteUser
        {
            Id = "user-1",
            UserName = "member@example.com",
            Status = UserStatus.OPRETTET
        });
        var offer = new PaymentOffer
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
        };
        db.PaymentOffers.Add(offer);
        await db.SaveChangesAsync();

        var product = new Stripe.Product { Id = "prod_membership", Name = "Kontingent" };
        var price = new Stripe.Price
        {
            Id = "price_membership",
            ProductId = product.Id,
            Type = "one_time",
            UnitAmount = 15000,
            Currency = "dkk"
        };
        var stripe = new Mock<IStripeService>();
        stripe.Setup(value => value.GetProductByExactNameAsync("Kontingent")).ReturnsAsync(product);
        stripe.Setup(value => value.GetPriceAsync(product.Id)).ReturnsAsync((Stripe.Price)null);
        stripe.Setup(value => value.CreateOneTimePriceAsync(product.Id, 15000, "dkk")).ReturnsAsync(price);
        stripe.Setup(value => value.GetProductAsync(product.Id)).ReturnsAsync(product);
        stripe.Setup(value => value.GetPriceByIdAsync(price.Id)).ReturnsAsync(price);
        stripe.Setup(value => value.StartPayment(
                product,
                price,
                "user-1",
                It.IsAny<string>(),
                It.IsAny<string>(),
                "payment",
                It.IsAny<System.Collections.Generic.IDictionary<string, string>>()))
            .Returns("cs_membership");

        var controller = new ApiPaymentOffersController(
            db,
            stripe.Object,
            new Mock<IAccessControlService>().Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "user-1")
                }, "test"))
            }
        };

        var result = Assert.IsType<OkObjectResult>(await controller.Checkout(offer.Id, default));

        Assert.Contains("cs_membership", result.Value.ToString());
        var savedOffer = await db.PaymentOffers.SingleAsync();
        Assert.Equal(product.Id, savedOffer.ProviderProductId);
        Assert.Equal(price.Id, savedOffer.ProviderPriceId);
    }

    [Fact]
    public async Task SuccessContextMarksCompletedStripeMembershipPaymentAsPaid()
    {
        await using var db = CreateDatabase();
        db.Users.Add(new SiteUser
        {
            Id = "user-1",
            UserName = "member@example.com",
            Status = UserStatus.OPRETTET
        });
        db.PaymentOffers.Add(new PaymentOffer
        {
            Scope = PaymentOfferScopes.Membership,
            Provider = PaymentOfferProviders.Stripe,
            OfferType = PaymentOfferTypes.OneTime,
            ProductName = "Kontingent",
            IsActive = true,
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var offer = await db.PaymentOffers.SingleAsync();
        var session = new Session
        {
            Id = "cs_membership",
            Mode = "payment",
            ClientReferenceId = "user-1",
            Created = DateTime.UtcNow,
            Metadata = new System.Collections.Generic.Dictionary<string, string>
            {
                ["PaymentOfferId"] = offer.Id.ToString()
            }
        };
        var stripe = new Mock<IStripeService>();
        stripe.Setup(value => value.GetSessionAsync("cs_membership")).ReturnsAsync(session);
        stripe.Setup(value => value.IsPaymentComplete(session)).Returns(true);

        var controller = new ApiPaymentOffersController(
            db,
            stripe.Object,
            new Mock<IAccessControlService>().Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "user-1")
                }, "test"))
            }
        };

        var result = Assert.IsType<OkObjectResult>(await controller.GetSuccessContext("cs_membership"));

        Assert.Contains("MEMBERSHIP", result.Value.ToString());
        var user = await db.Users.SingleAsync();
        Assert.Equal(UserStatus.BETALT, user.Status);
    }

    private static ApplicationDbContext CreateDatabase()
    {
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    private static async Task<PaymentOffer> SeedOfferAsync(ApplicationDbContext db)
    {
        var item = new ContentItem
        {
            Type = ContentTypes.Event,
            Status = ContentStatuses.Published,
            Title = "Test event",
            Slug = "test-event",
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        };
        var offer = new PaymentOffer
        {
            ContentItem = item,
            Scope = PaymentOfferScopes.Event,
            Provider = PaymentOfferProviders.Stripe,
            OfferType = PaymentOfferTypes.OneTime,
            ProductName = "Test product",
            ProviderPriceId = "price_event",
            IsActive = true,
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        };
        db.PaymentOffers.Add(offer);
        await db.SaveChangesAsync();
        item.Links.Add(new ContentLink
        {
            Label = "Betalingslink",
            Url = $"/react/events/test-event/pay/{offer.Id}",
            Type = "PAYMENT",
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        });
        item.Links.Add(new ContentLink
        {
            Label = "Ekstern betaling",
            Url = "https://tickets.example.com/event",
            Type = "PAYMENT",
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        return offer;
    }
}
