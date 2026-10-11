using System;
using System.Security.Claims;
using System.Threading.Tasks;
using GamMaSite.Data;
using GamMaSite.Models;
using GamMaSite.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GamMaSite.Tests;

public class AccessControlServiceTests
{
    [Fact]
    public async Task EventOrganizerCanEditOnlyTheAssignedEvent()
    {
        await using var db = CreateDb();
        var assignedEvent = Event(3, "assigned-event");
        db.ContentItems.AddRange(assignedEvent, Event(4, "other-event"));
        db.EventRegistrations.Add(new EventRegistration
        {
            ContentItemId = 3,
            UserId = "organizer",
            RegistrationType = RegistrationTypes.Organizer,
            Registered = true,
            ContentItem = assignedEvent
        });
        await db.SaveChangesAsync();

        var service = new AccessControlService(db);
        var user = User("organizer");

        Assert.True(await service.IsEventOrganizerAsync(user, 3));
        Assert.True(await service.CanEditEventAsync(user, 3));
        Assert.True(await service.CanEditRegistrationsAsync(user, 3));
        Assert.False(await service.IsEventOrganizerAsync(user, 4));
        Assert.False(await service.CanEditEventAsync(user, 4));
    }

    [Fact]
    public async Task InactiveOrganizerRegistrationDoesNotGrantAccess()
    {
        await using var db = CreateDb();
        var eventItem = Event(3, "assigned-event");
        db.ContentItems.Add(eventItem);
        db.EventRegistrations.Add(new EventRegistration
        {
            ContentItemId = eventItem.Id,
            UserId = "organizer",
            RegistrationType = RegistrationTypes.Organizer,
            Registered = false,
            ContentItem = eventItem
        });
        await db.SaveChangesAsync();

        var service = new AccessControlService(db);

        Assert.False(await service.CanEditEventAsync(User("organizer"), eventItem.Id));
    }

    private static ClaimsPrincipal User(string id) => new(new ClaimsIdentity(
        new[] { new Claim(ClaimTypes.NameIdentifier, id) },
        "test"));

    private static ContentItem Event(int id, string slug) => new()
    {
        Id = id,
        Title = slug,
        Slug = slug,
        Type = ContentTypes.Event,
        Status = ContentStatuses.Published
    };

    private static ApplicationDbContext CreateDb() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
