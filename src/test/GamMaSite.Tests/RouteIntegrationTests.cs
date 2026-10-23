using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace GamMaSite.Tests;

public class RouteIntegrationTests : IClassFixture<RouteTestApplicationFactory>
{
    private readonly HttpClient _client;

    public RouteIntegrationTests(RouteTestApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Theory]
    [InlineData("/react/library", "/Library")]
    [InlineData("/bibliotek", "/Library")]
    [InlineData("/library/", "/Library")]
    public async Task LegacyLibraryRoutesRedirectToCanonicalLibrary(string path, string expectedLocation)
    {
        using var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal(expectedLocation, response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task UnknownApiRouteReturnsNotFoundInsteadOfReactShell()
    {
        using var response = await _client.GetAsync("/api/route-that-does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotEqual("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task KnownApiRouteIsNotCapturedByApiFallback()
    {
        using var response = await _client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }
}

public sealed class RouteTestApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseTestServer();
        builder.UseSetting("PublicBaseUrl", "https://localhost:5001");
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Server=127.0.0.1;Port=3306;Database=gammasite_route_tests;User Id=test;Password=test;");
        builder.UseSetting("MobilePay:BaseUrl", "https://example.com");
        builder.UseSetting("GitHub:ContentAPI", "https://api.github.com/repos/example/example/contents/");
    }
}
