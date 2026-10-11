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
