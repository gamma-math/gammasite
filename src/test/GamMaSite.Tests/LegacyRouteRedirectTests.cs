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

    [Theory]
    [InlineData("/library")]
    [InlineData("/library/")]
    [InlineData("/LIBRARY/")]
    public void IsNonCanonicalLibraryPath_RecognizesLegacyLibraryVariants(string path)
    {
        Assert.True(LegacyRouteRedirects.IsNonCanonicalLibraryPath(path));
    }

    [Fact]
    public void IsNonCanonicalLibraryPath_LeavesCanonicalPathUntouched()
    {
        Assert.False(LegacyRouteRedirects.IsNonCanonicalLibraryPath("/Library"));
    }

    [Theory]
    [InlineData("library/", "/Library")]
    [InlineData("Library/Index/", "/Library")]
    [InlineData("react/library/", "/react/library")]
    public void BuildLocation_NormalizesTrailingSlashes(string path, string expectedPath)
    {
        Assert.Equal(expectedPath, LegacyRouteRedirects.BuildLocation(path));
    }
}
