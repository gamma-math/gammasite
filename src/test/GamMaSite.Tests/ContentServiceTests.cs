using System;
using System.Linq;
using System.Threading.Tasks;
using GamMaSite.Data;
using GamMaSite.Models;
using GamMaSite.Services;
using GamMaSite.ViewModels.Api;
using Microsoft.EntityFrameworkCore;
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

    private static ContentItem Item(string slug, DateTime start) => new()
    {
        Title = slug, Slug = slug, Type = ContentTypes.Event, Status = ContentStatuses.Published,
        StartDate = start, Created = start, PublishedAt = start
    };

    private static ApplicationDbContext CreateDb() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
