using System;
using System.Threading.Tasks;
using GamMaSite.Data;
using GamMaSite.Models;
using GamMaSite.Services;
using GamMaSite.ViewModels.Api;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GamMaSite.Tests;

/*
 * Tests the event registration service used by the React event registration APIs.
 * Covers published/open event rules, registration status normalization, and declines.
 */
public class EventRegistrationServiceTests
{
    [Fact]
    public async Task RegisterAsync_CreatesRegistrationForPublishedOpenEvent()
    {
        await using var db = CreateDb();
        db.Users.Add(new SiteUser { Id = "user-1", UserName = "user-1", Email = "user@example.com" });
        db.ContentItems.Add(Event(ContentStatuses.Published, DateTime.UtcNow.AddDays(1)));
        await db.SaveChangesAsync();
        var service = new EventRegistrationService(db);

        var registration = await service.RegisterAsync(1, "user-1", new SaveEventRegistrationRequest { RegistrationType = "attendee" });

        Assert.Equal(RegistrationTypes.Attendee, registration.RegistrationType);
        Assert.True(registration.Registered);
    }

    [Theory]
    [InlineData(ContentStatuses.Draft)]
    [InlineData(ContentStatuses.Archived)]
    public async Task RegisterAsync_RejectsUnpublishedEvent(string status)
    {
        await using var db = CreateDb();
        db.Users.Add(new SiteUser { Id = "user-1", UserName = "user-1", Email = "user@example.com" });
        db.ContentItems.Add(Event(status, DateTime.UtcNow.AddDays(1)));
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ArgumentException>(() => new EventRegistrationService(db)
            .RegisterAsync(1, "user-1", new SaveEventRegistrationRequest()));
    }

    [Fact]
    public async Task RegisterAsync_RejectsClosedEvent()
    {
        await using var db = CreateDb();
        db.Users.Add(new SiteUser { Id = "user-1", UserName = "user-1", Email = "user@example.com" });
        db.ContentItems.Add(Event(ContentStatuses.Published, DateTime.UtcNow.AddMinutes(-1)));
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ArgumentException>(() => new EventRegistrationService(db)
            .RegisterAsync(1, "user-1", new SaveEventRegistrationRequest()));
    }

    [Fact]
    public async Task RegisterAsync_DeclinedRegistrationIsNotRegistered()
    {
        await using var db = CreateDb();
        db.Users.Add(new SiteUser { Id = "user-1", UserName = "user-1", Email = "user@example.com" });
        db.ContentItems.Add(Event(ContentStatuses.Published, DateTime.UtcNow.AddDays(1)));
        await db.SaveChangesAsync();

        var registration = await new EventRegistrationService(db).RegisterAsync(1, "user-1",
            new SaveEventRegistrationRequest { RegistrationType = RegistrationTypes.Declined });

        Assert.False(registration.Registered);
    }

    [Fact]
    public async Task UnregisterAsync_ReturnsFalseWhenUserHasNoRegistration()
    {
        await using var db = CreateDb();
        db.ContentItems.Add(Event(ContentStatuses.Published, DateTime.UtcNow.AddDays(1)));
        await db.SaveChangesAsync();

        var result = await new EventRegistrationService(db).UnregisterAsync(1, "missing-user");

        Assert.False(result);
    }

    [Fact]
    public async Task AddAsync_RejectsUnknownUser()
    {
        await using var db = CreateDb();
        db.ContentItems.Add(Event(ContentStatuses.Published, DateTime.UtcNow.AddDays(1)));
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ArgumentException>(() => new EventRegistrationService(db).AddAsync(1,
            new AddEventRegistrationRequest { UserId = "missing-user" }));
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNullForUnknownRegistration()
    {
        await using var db = CreateDb();

        var result = await new EventRegistrationService(db).UpdateAsync(1, 99,
            new UpdateEventRegistrationRequest { RegistrationType = RegistrationTypes.Attendee });

        Assert.Null(result);
    }

    [Fact]
    public async Task RegisterAsync_RejectsUnknownRegistrationType()
    {
        await using var db = CreateDb();
        db.Users.Add(new SiteUser { Id = "user-1", UserName = "user-1" });
        db.ContentItems.Add(Event(ContentStatuses.Published, DateTime.UtcNow.AddDays(1)));
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ArgumentException>(() => new EventRegistrationService(db).RegisterAsync(1, "user-1",
            new SaveEventRegistrationRequest { RegistrationType = "UNKNOWN" }));
    }

    private static ContentItem Event(string status, DateTime endDate) => new()
    {
        Title = "Event", Slug = "event", Type = ContentTypes.Event, Status = status,
        StartDate = DateTime.UtcNow.AddHours(-1), EndDate = endDate
    };

    private static ApplicationDbContext CreateDb() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
