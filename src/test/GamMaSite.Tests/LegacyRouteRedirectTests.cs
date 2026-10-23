using GamMaSite.Services;
using Xunit;

namespace GamMaSite.Tests;

public class LegacyRouteRedirectTests
{
    [Theory]
    [InlineData("", "/")]
    [InlineData("events/test-event", "/events/test-event")]
    [InlineData("bibliotek", "/Library")]
    [InlineData("referater", "/Library")]
    [InlineData("library", "/Library")]
    [InlineData("Library/Index", "/Library")]
    [InlineData("Library/IndexAsync", "/Library")]
    public void BuildLocation_MapsLegacyPathToCanonicalPath(string legacyPath, string expectedPath)
    {
        Assert.Equal(expectedPath, LegacyRouteRedirects.BuildLocation(legacyPath));
    }

    [Fact]
    public void BuildLocation_PreservesQueryString()
    {
        Assert.Equal(
            "/events/test-event?returnUrl=%2Faccount%2Fmanage",
            LegacyRouteRedirects.BuildLocation("events/test-event", "?returnUrl=%2Faccount%2Fmanage"));
    }
}
