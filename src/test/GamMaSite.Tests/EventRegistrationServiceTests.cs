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
        Assert.False(registration.Registered);
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
    public async Task RegisterAsync_PutsAttendeeOnWaitlistWhenCapacityIsReached()
    {
        await using var db = CreateDb();
        db.Users.AddRange(
            new SiteUser { Id = "user-1", UserName = "user-1" },
            new SiteUser { Id = "user-2", UserName = "user-2" });
        db.ContentItems.Add(Event(ContentStatuses.Published, DateTime.UtcNow.AddDays(1), 1));
        await db.SaveChangesAsync();
        var service = new EventRegistrationService(db);

        var confirmed = await service.RegisterAsync(1, "user-1", new SaveEventRegistrationRequest());
        var waitlisted = await service.RegisterAsync(1, "user-2", new SaveEventRegistrationRequest());

        Assert.Equal(RegistrationTypes.Attendee, confirmed.RegistrationType);
        Assert.Equal(RegistrationTypes.Waitlisted, waitlisted.RegistrationType);
        Assert.False(waitlisted.Registered);
    }

    [Fact]
    public async Task AddAsync_RejectsConfirmedRegistrationWhenCapacityIsReached()
    {
        await using var db = CreateDb();
        db.Users.AddRange(
            new SiteUser { Id = "user-1", UserName = "user-1" },
            new SiteUser { Id = "user-2", UserName = "user-2" });
        db.ContentItems.Add(Event(ContentStatuses.Published, DateTime.UtcNow.AddDays(1), 1));
        await db.SaveChangesAsync();
        var service = new EventRegistrationService(db);

        var added = await service.AddAsync(1, new AddEventRegistrationRequest { UserId = "user-1" });
        Assert.False(added.Registered);
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.AddAsync(1,
            new AddEventRegistrationRequest { UserId = "user-2", RegistrationType = RegistrationTypes.Attendee, Registered = true }));

        Assert.Equal("Der er allerede maks antal deltagere.", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_RejectsChangingWaitlistedRegistrationToConfirmedWhenCapacityIsReached()
    {
        await using var db = CreateDb();
        db.Users.AddRange(
            new SiteUser { Id = "user-1", UserName = "user-1" },
            new SiteUser { Id = "user-2", UserName = "user-2" });
        db.ContentItems.Add(Event(ContentStatuses.Published, DateTime.UtcNow.AddDays(1), 1));
        db.EventRegistrations.AddRange(
            new EventRegistration { ContentItemId = 1, UserId = "user-1", RegistrationType = RegistrationTypes.Attendee, Registered = true },
            new EventRegistration { ContentItemId = 1, UserId = "user-2", RegistrationType = RegistrationTypes.Waitlisted, Registered = true });
        await db.SaveChangesAsync();

        var registration = await db.EventRegistrations.SingleAsync(item => item.UserId == "user-2");
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => new EventRegistrationService(db).UpdateAsync(1, registration.Id,
            new UpdateEventRegistrationRequest { RegistrationType = RegistrationTypes.Attendee, Registered = true }));

        Assert.Equal("Der er allerede maks antal deltagere.", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_AllowsChangingParticipantToWaitlist()
    {
        await using var db = CreateDb();
        db.Users.Add(new SiteUser { Id = "user-1", UserName = "user-1" });
        db.ContentItems.Add(Event(ContentStatuses.Published, DateTime.UtcNow.AddDays(1), 1));
        db.EventRegistrations.Add(new EventRegistration
        {
            ContentItemId = 1,
            UserId = "user-1",
            RegistrationType = RegistrationTypes.Attendee,
            Registered = false
        });
        await db.SaveChangesAsync();

        var registration = await db.EventRegistrations.SingleAsync();
        var updated = await new EventRegistrationService(db).UpdateAsync(1, registration.Id,
            new UpdateEventRegistrationRequest { RegistrationType = RegistrationTypes.Waitlisted, Registered = false });

        Assert.Equal(RegistrationTypes.Waitlisted, updated.RegistrationType);
        Assert.False(updated.Registered);
    }

    [Fact]
    public async Task UnregisterAsync_PromotesOldestWaitlistedRegistration()
    {
        await using var db = CreateDb();
        db.Users.AddRange(
            new SiteUser { Id = "user-1", UserName = "user-1" },
            new SiteUser { Id = "user-2", UserName = "user-2" },
            new SiteUser { Id = "user-3", UserName = "user-3" });
        db.ContentItems.Add(Event(ContentStatuses.Published, DateTime.UtcNow.AddDays(1), 1));
        db.EventRegistrations.AddRange(
            new EventRegistration { ContentItemId = 1, UserId = "user-1", RegistrationType = RegistrationTypes.Attendee, Registered = true },
            new EventRegistration { ContentItemId = 1, UserId = "user-2", RegistrationType = RegistrationTypes.Waitlisted, Registered = true, Created = DateTime.UtcNow.AddMinutes(-2) },
            new EventRegistration { ContentItemId = 1, UserId = "user-3", RegistrationType = RegistrationTypes.Waitlisted, Registered = true, Created = DateTime.UtcNow.AddMinutes(-1) });
        await db.SaveChangesAsync();

        var result = await new EventRegistrationService(db).UnregisterAsync(1, "user-1");

        Assert.True(result);
        Assert.Equal(RegistrationTypes.Attendee, (await db.EventRegistrations.SingleAsync(item => item.UserId == "user-2")).RegistrationType);
        Assert.Equal(RegistrationTypes.Waitlisted, (await db.EventRegistrations.SingleAsync(item => item.UserId == "user-3")).RegistrationType);
    }

    [Fact]
    public async Task UpdateAsync_PromotesWaitlistedRegistrationWhenConfirmedPlaceIsReleased()
    {
        await using var db = CreateDb();
        db.Users.AddRange(
            new SiteUser { Id = "user-1", UserName = "user-1" },
            new SiteUser { Id = "user-2", UserName = "user-2" });
        db.ContentItems.Add(Event(ContentStatuses.Published, DateTime.UtcNow.AddDays(1), 1));
        db.EventRegistrations.AddRange(
            new EventRegistration { ContentItemId = 1, UserId = "user-1", RegistrationType = RegistrationTypes.Attendee, Registered = true },
            new EventRegistration { ContentItemId = 1, UserId = "user-2", RegistrationType = RegistrationTypes.Waitlisted, Registered = true, Created = DateTime.UtcNow.AddMinutes(-1) });
        await db.SaveChangesAsync();

        var confirmed = await db.EventRegistrations.SingleAsync(item => item.UserId == "user-1");
        await new EventRegistrationService(db).UpdateAsync(1, confirmed.Id,
            new UpdateEventRegistrationRequest { RegistrationType = RegistrationTypes.Interested, Registered = true });

        Assert.Equal(RegistrationTypes.Attendee, (await db.EventRegistrations.SingleAsync(item => item.UserId == "user-2")).RegistrationType);
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
    public async Task DeleteAsync_RemovesRegistrationById()
    {
        await using var db = CreateDb();
        db.Users.Add(new SiteUser { Id = "user-1", UserName = "user-1", Email = "user@example.com" });
        db.ContentItems.Add(Event(ContentStatuses.Published, DateTime.UtcNow.AddDays(1)));
        db.EventRegistrations.Add(new EventRegistration
        {
            ContentItemId = 1,
            UserId = "user-1",
            RegistrationType = RegistrationTypes.Attendee,
            Registered = true
        });
        await db.SaveChangesAsync();

        var deleted = await new EventRegistrationService(db).DeleteAsync(1, 1);

        Assert.True(deleted);
        Assert.Empty(await db.EventRegistrations.ToListAsync());
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
    public async Task UpdateAsync_AllowsExistingRegistrationAfterEventHasClosed()
    {
        await using var db = CreateDb();
        db.Users.Add(new SiteUser { Id = "user-1", UserName = "user-1", Email = "user@example.com" });
        db.ContentItems.Add(Event(ContentStatuses.Published, DateTime.UtcNow.AddMinutes(-1)));
        db.EventRegistrations.Add(new EventRegistration
        {
            ContentItemId = 1,
            UserId = "user-1",
            RegistrationType = RegistrationTypes.Attendee,
            Registered = true
        });
        await db.SaveChangesAsync();

        var result = await new EventRegistrationService(db).UpdateAsync(1, 1,
            new UpdateEventRegistrationRequest { RegistrationType = RegistrationTypes.Organizer, Registered = false });

        Assert.Equal(RegistrationTypes.Organizer, result.RegistrationType);
        Assert.False(result.Registered);
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

    private static ContentItem Event(string status, DateTime endDate, int? maxParticipants = null) => new()
    {
        Title = "Event", Slug = "event", Type = ContentTypes.Event, Status = status,
        StartDate = DateTime.UtcNow.AddHours(-1), EndDate = endDate, MaxParticipants = maxParticipants
    };

    private static ApplicationDbContext CreateDb() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
