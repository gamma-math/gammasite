using System;
using System.Linq;
using System.Threading.Tasks;
using GamMaSite.Controllers;
using GamMaSite.Data;
using GamMaSite.Models;
using GamMaSite.Services;
using GamMaSite.ViewModels.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
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
