using System;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace GamMaSite.Services
{
    public sealed class PlaywrightReceiptPdfService : IReceiptPdfService
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<PlaywrightReceiptPdfService> _logger;

        public PlaywrightReceiptPdfService(
            IConfiguration configuration,
            IWebHostEnvironment environment,
            ILogger<PlaywrightReceiptPdfService> logger)
        {
            _configuration = configuration;
            _environment = environment;
            _logger = logger;
        }

        public async Task<byte[]> RenderAsync(string html, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                throw new ArgumentException("Receipt HTML is required.", nameof(html));
            }

            try
            {
                var playwright = await Playwright.CreateAsync();
                try
                {
                    var executablePath = ResolveChromiumExecutablePath();
                    _logger.LogDebug("Rendering receipt PDF with Chromium executable: {ExecutablePath}", executablePath ?? "Playwright bundled browser");

                    var launchOptions = new BrowserTypeLaunchOptions
                    {
                        Headless = true,
                        ExecutablePath = executablePath,
                        Args = new[] { "--no-sandbox", "--disable-dev-shm-usage" }
                    };

                    await using var browser = await playwright.Chromium.LaunchAsync(launchOptions);
                    var page = await browser.NewPageAsync(new BrowserNewPageOptions
                    {
                        ViewportSize = new ViewportSize { Width = 794, Height = 1123 }
                    });
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var document = AddBaseUrl(InlineLocalLogo(html));
                        await page.SetContentAsync(document, new PageSetContentOptions
                        {
                            WaitUntil = WaitUntilState.NetworkIdle,
                            Timeout = 30_000
                        });
                        await page.WaitForFunctionAsync(
                            "() => Array.from(document.images).every(image => image.complete && image.naturalWidth > 0)",
                            new PageWaitForFunctionOptions { Timeout = 10_000 });
                        await page.EmulateMediaAsync(new PageEmulateMediaOptions { Media = Media.Print });

                        cancellationToken.ThrowIfCancellationRequested();
                        return await page.PdfAsync(new PagePdfOptions
                        {
                            Format = "A4",
                            PrintBackground = true,
                            PreferCSSPageSize = true
                        });
                    }
                    finally
                    {
                        await page.CloseAsync();
                    }
                }
                finally
                {
                    playwright.Dispose();
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Receipt PDF rendering failed.");
                throw new InvalidOperationException(
                    "PDF-rendering er ikke tilgængelig. Kontrollér, at Chromium er installeret i deployet.",
                    exception);
            }
        }

        private string InlineLocalLogo(string html)
        {
            var webRootPath = _environment.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
            }

            var logoPath = Path.Combine(webRootPath, "lib", "logo_blue.png");
            if (!File.Exists(logoPath))
            {
                return html;
            }

            var logoDataUri = "data:image/png;base64," + Convert.ToBase64String(File.ReadAllBytes(logoPath));
            return html
                .Replace("https://gam-ma.dk/lib/logo_blue.png", logoDataUri, StringComparison.OrdinalIgnoreCase)
                .Replace("/lib/logo_blue.png", logoDataUri, StringComparison.OrdinalIgnoreCase);
        }

        private string AddBaseUrl(string html)
        {
            var publicBaseUrl = (_configuration["Pdf:BaseUrl"] ?? _configuration["PublicBaseUrl"])?.TrimEnd('/') + "/";
            if (!Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out var baseUri) ||
                (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
            {
                return html;
            }

            var encodedBaseUrl = HtmlEncoder.Default.Encode(baseUri.ToString());
            return Regex.Replace(
                html,
                "<head(\\s[^>]*)?>",
                match => $"{match.Value}<base href=\"{encodedBaseUrl}\">",
                RegexOptions.IgnoreCase,
                TimeSpan.FromSeconds(1));
        }

        private string ResolveChromiumExecutablePath()
        {
            var configuredPath = _configuration["Pdf:ChromiumExecutablePath"];
            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                return configuredPath;
            }

            var candidates = OperatingSystem.IsWindows()
                ? new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "Application", "chrome.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "Edge", "Application", "msedge.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", "msedge.exe")
                }
                : new[] { "/usr/bin/chromium", "/usr/bin/chromium-browser", "/usr/bin/google-chrome" };

            foreach (var candidate in candidates)
            {
                if (!string.IsNullOrWhiteSpace(candidate) && File.Exists(candidate))
                {
                    return candidate;
                }
            }

            var bundledBrowserRoot = Path.Combine(
                _environment.ContentRootPath,
                ".playwright",
                "package",
                ".local-browsers");
            if (Directory.Exists(bundledBrowserRoot))
            {
                var bundledExecutableName = OperatingSystem.IsWindows() ? "chrome.exe" : "chrome";
                var bundledExecutable = Directory.EnumerateFiles(
                        bundledBrowserRoot,
                        bundledExecutableName,
                        SearchOption.AllDirectories)
                    .FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(bundledExecutable))
                {
                    return bundledExecutable;
                }
            }

            return null;
        }
    }
}
