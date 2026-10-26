using System;
using System.Linq;
using System.Threading.Tasks;
using GamMaSite.Data;
using GamMaSite.Models;
using GamMaSite.Services;
using GamMaSite.ViewModels.Api;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace GamMaSite.Tests;

/*
 * Tests the content service used by the React event and news APIs.
 * Covers event ordering and the HTTPS-only validation of related content links.
 */
public class ContentServiceTests
{
    [Fact]
    public async Task GetPublishedEvents_OrdersFutureEventsBeforePastEvents()
    {
        await using var db = CreateDb();
        db.ContentItems.AddRange(
            Item("past", DateTime.UtcNow.AddDays(-1)),
            Item("future-late", DateTime.UtcNow.AddDays(3)),
            Item("future-soon", DateTime.UtcNow.AddDays(1)));
        await db.SaveChangesAsync();

        var result = await new ContentService(db).GetPublishedAsync(ContentTypes.Event);

        Assert.Equal(new[] { "future-soon", "future-late", "past" }, result.Select(item => item.Slug));
    }

    [Fact]
    public async Task CreateAsync_RejectsNonHttpsRelatedLink()
    {
        await using var db = CreateDb();
        var request = new SaveContentItemRequest
        {
            Title = "News", Slug = "news", Type = ContentTypes.News, Status = ContentStatuses.Published,
            Links = { new SaveContentLinkRequest { Label = "Link", Url = "http://example.com", Type = "LINK" } }
        };

        await Assert.ThrowsAsync<ArgumentException>(() => new ContentService(db).CreateAsync(request, "admin"));
    }

    [Fact]
    public async Task CreateAsync_PersistsHttpsRelatedLink()
    {
        await using var db = CreateDb();
        var item = await new ContentService(db).CreateAsync(new SaveContentItemRequest
        {
            Title = "News", Slug = "news", Type = ContentTypes.News, Status = ContentStatuses.Published,
            Links = { new SaveContentLinkRequest { Label = "Link", Url = "https://example.com", Type = "link" } }
        }, "admin");

        Assert.Equal("https://example.com", item.Links.Single().Url);
        Assert.Equal("LINK", item.Links.Single().Type);
    }

    [Fact]
    public async Task CreateAsync_PersistsEventCapacity()
    {
        await using var db = CreateDb();

        var item = await new ContentService(db).CreateAsync(new SaveContentItemRequest
        {
            Title = "Limited event", Slug = "limited-event", Type = ContentTypes.Event,
            Status = ContentStatuses.Published, MaxParticipants = 20
        }, "admin");

        Assert.Equal(20, item.MaxParticipants);
        var persistedItem = await db.ContentItems.FindAsync(item.Id);
        Assert.NotNull(persistedItem);
        Assert.Equal(20, persistedItem!.MaxParticipants);
    }

    [Fact]
    public async Task CreateAsync_RejectsNegativeEventCapacity()
    {
        await using var db = CreateDb();

        await Assert.ThrowsAsync<ArgumentException>(() => new ContentService(db).CreateAsync(new SaveContentItemRequest
        {
            Title = "Invalid event", Slug = "invalid-event", Type = ContentTypes.Event,
            Status = ContentStatuses.Published, MaxParticipants = -1
        }, "admin"));
    }

    [Fact]
    public async Task DeleteAsync_RemovesEventRelationsAndArchivesStripeDefinitions()
    {
        await using var db = CreateDb();
        var item = Item("delete-event", DateTime.UtcNow);
        db.ContentItems.Add(item);
        await db.SaveChangesAsync();

        db.ContentLinks.Add(new ContentLink
        {
            ContentItemId = item.Id,
            Label = "Facebook",
            Url = "https://example.com/event",
            Type = "LINK"
        });
        db.EventRegistrations.Add(new EventRegistration
        {
            ContentItemId = item.Id,
            UserId = "user-1",
            RegistrationType = RegistrationTypes.Attendee
        });
        db.PaymentOffers.Add(new PaymentOffer
        {
            ContentItemId = item.Id,
            Scope = PaymentOfferScopes.Event,
            Provider = PaymentOfferProviders.Stripe,
            OfferType = PaymentOfferTypes.OneTime,
            ProductName = "Event ticket",
            ProviderProductId = "prod_event",
            ProviderPriceId = "price_event"
        });
        await db.SaveChangesAsync();

        var stripe = new Mock<IStripeService>();
        stripe.Setup(service => service.DeactivatePriceAsync("price_event")).ReturnsAsync(true);
        stripe.Setup(service => service.DeactivateProductAsync("prod_event")).ReturnsAsync(true);

        var deleted = await new ContentService(db, stripeService: stripe.Object).DeleteAsync(item.Id);

        Assert.True(deleted);
        Assert.Null(await db.ContentItems.FindAsync(item.Id));
        Assert.Empty(await db.ContentLinks.Where(link => link.ContentItemId == item.Id).ToListAsync());
        Assert.Empty(await db.EventRegistrations.Where(registration => registration.ContentItemId == item.Id).ToListAsync());
        Assert.Empty(await db.PaymentOffers.Where(offer => offer.ContentItemId == item.Id).ToListAsync());
        stripe.Verify(service => service.DeactivatePriceAsync("price_event"), Times.Once);
        stripe.Verify(service => service.DeactivateProductAsync("prod_event"), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_RestoresLocalContentWhenStripeArchivingFails()
    {
        await using var db = CreateDb();
        var item = Item("delete-event-failed", DateTime.UtcNow);
        db.ContentItems.Add(item);
        await db.SaveChangesAsync();
        db.PaymentOffers.Add(new PaymentOffer
        {
            ContentItemId = item.Id,
            Scope = PaymentOfferScopes.Event,
            Provider = PaymentOfferProviders.Stripe,
            OfferType = PaymentOfferTypes.OneTime,
            ProductName = "Event ticket",
            ProviderProductId = "prod_event_failed",
            ProviderPriceId = "price_event_failed"
        });
        await db.SaveChangesAsync();

        var stripe = new Mock<IStripeService>();
        stripe.Setup(service => service.DeactivatePriceAsync("price_event_failed")).ReturnsAsync(true);
        stripe.Setup(service => service.DeactivateProductAsync("prod_event_failed")).ReturnsAsync(false);
        stripe.Setup(service => service.SetPriceActiveAsync("price_event_failed", true)).ReturnsAsync(true);

        await Assert.ThrowsAsync<ContentDeletionException>(() =>
            new ContentService(db, stripeService: stripe.Object).DeleteAsync(item.Id));

        Assert.NotNull(await db.ContentItems.FindAsync(item.Id));
        Assert.Single(await db.PaymentOffers.Where(offer => offer.ContentItemId == item.Id).ToListAsync());
        stripe.Verify(service => service.SetPriceActiveAsync("price_event_failed", true), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_RejectsCapacityBelowExistingParticipants()
    {
        await using var db = CreateDb();
        db.Users.AddRange(
            new SiteUser { Id = "user-1", UserName = "user-1" },
            new SiteUser { Id = "user-2", UserName = "user-2" });
        var item = Item("capacity-event", DateTime.UtcNow);
        db.ContentItems.Add(item);
        db.EventRegistrations.AddRange(
            new EventRegistration { ContentItem = item, UserId = "user-1", RegistrationType = RegistrationTypes.Attendee, Registered = false },
            new EventRegistration { ContentItem = item, UserId = "user-2", RegistrationType = RegistrationTypes.Organizer, Registered = false });
        await db.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => new ContentService(db).UpdateAsync(item.Id, new SaveContentItemRequest
        {
            Title = item.Title,
            Slug = item.Slug,
            Type = ContentTypes.Event,
            Status = ContentStatuses.Published,
            MaxParticipants = 1,
            StartDate = item.StartDate
        }));

        Assert.Equal("Der angivne maks. antal deltagere er lavere end det nuværende antal deltagere og arrangører.", exception.Message);
        var persistedItem = await db.ContentItems.FindAsync(item.Id);
        Assert.NotNull(persistedItem);
        Assert.Null(persistedItem!.MaxParticipants);
    }

    [Fact]
    public async Task UpdateAsync_AllowsCapacityAtOrAboveExistingParticipants()
    {
        await using var db = CreateDb();
        db.Users.AddRange(
            new SiteUser { Id = "user-1", UserName = "user-1" },
            new SiteUser { Id = "user-2", UserName = "user-2" });
        var item = Item("capacity-event", DateTime.UtcNow);
        db.ContentItems.Add(item);
        db.EventRegistrations.AddRange(
            new EventRegistration { ContentItem = item, UserId = "user-1", RegistrationType = RegistrationTypes.Attendee },
            new EventRegistration { ContentItem = item, UserId = "user-2", RegistrationType = RegistrationTypes.Organizer });
        await db.SaveChangesAsync();

        var result = await new ContentService(db).UpdateAsync(item.Id, new SaveContentItemRequest
        {
            Title = item.Title,
            Slug = item.Slug,
            Type = ContentTypes.Event,
            Status = ContentStatuses.Published,
            MaxParticipants = 2,
            StartDate = item.StartDate
        });

        Assert.Equal(2, result.MaxParticipants);
    }

    [Fact]
    public async Task UpdateAsync_PromotesWaitlistedMembersWhenCapacityIncreases()
    {
        await using var db = CreateDb();
        db.Users.AddRange(
            new SiteUser { Id = "user-1", UserName = "user-1" },
            new SiteUser { Id = "user-2", UserName = "user-2" },
            new SiteUser { Id = "user-3", UserName = "user-3" });
        var item = Item("capacity-event", DateTime.UtcNow);
        item.MaxParticipants = 1;
        db.ContentItems.Add(item);
        db.EventRegistrations.AddRange(
            new EventRegistration { ContentItem = item, UserId = "user-1", RegistrationType = RegistrationTypes.Attendee },
            new EventRegistration { ContentItem = item, UserId = "user-2", RegistrationType = RegistrationTypes.Waitlisted, Created = DateTime.UtcNow.AddMinutes(-2) },
            new EventRegistration { ContentItem = item, UserId = "user-3", RegistrationType = RegistrationTypes.Waitlisted, Created = DateTime.UtcNow.AddMinutes(-1) });
        await db.SaveChangesAsync();

        await new ContentService(db, eventRegistrations: new EventRegistrationService(db)).UpdateAsync(item.Id, new SaveContentItemRequest
        {
            Title = item.Title,
            Slug = item.Slug,
            Type = ContentTypes.Event,
            Status = ContentStatuses.Published,
            MaxParticipants = 3,
            StartDate = item.StartDate
        });

        var registrations = await db.EventRegistrations.OrderBy(registration => registration.Created).ToListAsync();
        Assert.Equal(RegistrationTypes.Attendee, registrations[1].RegistrationType);
        Assert.Equal(RegistrationTypes.Attendee, registrations[2].RegistrationType);
    }

    [Fact]
    public async Task UpdateAsync_SucceedsWhenLocalImageCleanupFails()
    {
        await using var db = CreateDb();
        var item = Item("event", DateTime.UtcNow);
        const string oldPictureUrl = "/media/content/events/event.jpg";
        item.PictureUrl = oldPictureUrl;
        db.ContentItems.Add(item);
        await db.SaveChangesAsync();
        var media = new Mock<IContentMediaService>();
        media.Setup(service => service.DeleteLocalFileIfUnreferencedAsync(oldPictureUrl))
            .ThrowsAsync(new ContentMediaStorageException("File is locked"));

        var result = await new ContentService(db, media.Object).UpdateAsync(item.Id, new SaveContentItemRequest
        {
            Title = "Updated event",
            Slug = item.Slug,
            Type = ContentTypes.Event,
            Status = ContentStatuses.Published,
            PictureUrl = "https://images.example/event.jpg",
            StartDate = item.StartDate
        });

        Assert.NotNull(result);
        Assert.Equal("https://images.example/event.jpg", result.PictureUrl);
        media.Verify(service => service.DeleteLocalFileIfUnreferencedAsync(oldPictureUrl), Times.Exactly(2));
    }

    [Fact]
    public async Task UpdateAsync_RestoresSlugAndImageUrlWhenImageRenameFails()
    {
        await using var db = CreateDb();
        var item = Item("old-slug", DateTime.UtcNow);
        const string oldPictureUrl = "/media/content/events/old-slug.jpg";
        const string newPictureUrl = "/media/content/events/new-slug.jpg";
        item.PictureUrl = oldPictureUrl;
        db.ContentItems.Add(item);
        await db.SaveChangesAsync();

        var media = new Mock<IContentMediaService>();
        var rename = new ContentMediaSlugRename { PreviousUrl = oldPictureUrl, NewUrl = newPictureUrl };
        media.Setup(service => service.PrepareSlugRenameAsync(item, "old-slug", "new-slug"))
            .ReturnsAsync(rename);
        media.Setup(service => service.MoveLocalFileAsync(rename))
            .ThrowsAsync(new ContentMediaStorageException("File is locked"));

        await Assert.ThrowsAsync<ContentMediaStorageException>(() => new ContentService(db, media.Object).UpdateAsync(item.Id, new SaveContentItemRequest
        {
            Title = item.Title,
            Slug = "new-slug",
            Type = item.Type,
            Status = item.Status,
            PictureUrl = oldPictureUrl,
            StartDate = item.StartDate
        }));

        var saved = await db.ContentItems.FindAsync(item.Id);
        Assert.NotNull(saved);
        Assert.Equal("old-slug", saved.Slug);
        Assert.Equal(oldPictureUrl, saved.PictureUrl);
    }

    private static ContentItem Item(string slug, DateTime start) => new()
    {
        Title = slug, Slug = slug, Type = ContentTypes.Event, Status = ContentStatuses.Published,
        StartDate = start, Created = start, PublishedAt = start
    };

    private static ApplicationDbContext CreateDb() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
