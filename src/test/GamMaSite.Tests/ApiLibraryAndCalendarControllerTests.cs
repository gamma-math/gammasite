using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GamMaSite.Controllers;
using GamMaSite.Services;
using GamMaSite.ViewModels.Api;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GamMaSite.Tests;

/*
 * Tests the protected library and calendar endpoints used by React member pages.
 * Covers path forwarding, library item mapping, and empty calendar responses.
 */
public class ApiLibraryAndCalendarControllerTests
{
    [Fact]
    public async Task GetListing_ForwardsPathAndMapsFileType()
    {
        var index = new Mock<IIndexService>();
        index.Setup(value => value.GetContentMetasAsync("folder"))
            .ReturnsAsync(new List<ContentMeta> { new() { Name = "Document", Path = "folder/document.pdf", Type = "file" } });

        var result = await new ApiLibraryController(index.Object).GetListing("folder");

        var listing = Assert.IsType<LibraryListingDto>(Assert.IsType<OkObjectResult>(result).Value);
        var item = Assert.Single(listing.Items);
        Assert.Equal("Document", item.Name);
        Assert.Equal("🗎", item.Icon);
        index.Verify(value => value.GetContentMetasAsync("folder"), Times.Once);
    }

    [Fact]
    public async Task GetUpcoming_ReturnsEmptyWhenCalendarHasNoUpcomingEvents()
    {
        var calendar = new Mock<IICalService>();
        calendar.Setup(value => value.GetEventsWrapper()).ReturnsAsync(new EventsWrapper
        {
            Events = Enumerable.Empty<Ical.Net.CalendarComponents.CalendarEvent>()
        });

        var result = await new ApiCalendarController(calendar.Object).GetUpcoming();

        Assert.Empty(Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<CalendarEventDto>>(
            Assert.IsType<OkObjectResult>(result).Value));
    }
}
