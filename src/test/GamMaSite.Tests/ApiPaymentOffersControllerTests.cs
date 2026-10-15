using System;
using System.Linq;
using System.Threading.Tasks;
using GamMaSite.Controllers;
using GamMaSite.Data;
using GamMaSite.Models;
using GamMaSite.Services;
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
        access.Setup(value => value.CanEditEventAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>(), offer.ContentItemId))
            .ReturnsAsync(false);

        var stripe = new Mock<IStripeService>();
        var controller = new ApiPaymentOffersController(db, stripe.Object, access.Object);

        var result = await controller.Delete(offer.Id);

        Assert.IsType<ForbidResult>(result);
        Assert.NotNull(await db.PaymentOffers.FindAsync(offer.Id));
        stripe.Verify(value => value.DeactivatePriceAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Delete_RemovesOfferAndPaymentLinkAndDeactivatesStripePrice()
    {
        await using var db = CreateDatabase();
        var offer = await SeedOfferAsync(db);
        var access = new Mock<IAccessControlService>();
        access.Setup(value => value.CanEditEventAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>(), offer.ContentItemId))
            .ReturnsAsync(true);

        var stripe = new Mock<IStripeService>();
        stripe.Setup(value => value.DeactivatePriceAsync("price_event"))
            .ReturnsAsync(true);
        var controller = new ApiPaymentOffersController(db, stripe.Object, access.Object);

        var result = await controller.Delete(offer.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Null(await db.PaymentOffers.FindAsync(offer.Id));
        Assert.Empty(await db.ContentLinks.Where(link => link.ContentItemId == offer.ContentItemId).ToListAsync());
        stripe.Verify(value => value.DeactivatePriceAsync("price_event"), Times.Once);
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
        await db.SaveChangesAsync();
        return offer;
    }
}
