using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Stripe;
using GamMaSite.Data;
using GamMaSite.Models;
using GamMaSite.Services;


/* Create WebApplicationBuilder */
var builder = WebApplication.CreateBuilder(args);


/* ConfigureServices: Use this section to add services to the container. */
// For more: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/startup?view=aspnetcore-8.0

// Add the database connection
var mysqlConn = builder.Configuration.GetConnectionString("DefaultConnection");
var serverVersion = new MySqlServerVersion(new Version(8, 0));
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseMySql(mysqlConn, serverVersion));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Add HSTS
builder.Services.AddHsts(
    options =>
    {
        options.Preload = true;
        options.IncludeSubDomains = true;
        options.MaxAge = TimeSpan.FromDays(365);
    }
);

// Add Identity services
builder.Services.AddDefaultIdentity<SiteUser>(
    options => options.SignIn.RequireConfirmedAccount = true
)
.AddRoles<IdentityRole>()
.AddRoleManager<RoleManager<IdentityRole>>()
.AddErrorDescriber<DanishIdentityErrorDescriber>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Explicitly configure the auth cookie instead of relying on defaults.
// HttpOnly: blocks JS from reading the cookie (XSS mitigation).
// SameSite=Lax: blocks cross-site POST (CSRF) while allowing top-level GET navigation
//   (e.g. users clicking links in emails arrive already logged in).
// SecurePolicy=Always: default is SameAsRequest which omits Secure flag over HTTP.
// Name: avoids fingerprinting the server stack via the default cookie name.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
    options.Cookie.Name = ".GamMaSite.Auth";

    // By default, cookie authentication responds to 401/403 with a 302 redirect to
    // the HTML login page. This is correct for browser navigation (Identity Razor Pages),
    // but breaks fetch() calls from the React SPA: the browser silently follows the
    // redirect and the SPA receives HTML instead of JSON, causing parse errors.
    //
    // The fix is to intercept the redirect events and return the proper HTTP status codes
    // for API requests, while leaving the redirect behaviour intact for Razor Pages.
    // A request is treated as an API request if its path starts with /api (the hard
    // routing convention for all controllers) or if the caller explicitly signals it
    // accepts JSON (defensive catch-all for non-browser API clients).
    //
    // This is the standard pattern for mixed cookie + SPA apps in ASP.NET Core:
    // https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie#react-to-back-end-changes
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api") ||
            context.Request.Headers.Accept.ToString().Contains("application/json"))
        {
            // Unauthenticated — tell the SPA to redirect the user to the login page.
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }
        // Razor Pages (Identity): preserve the normal login redirect.
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api") ||
            context.Request.Headers.Accept.ToString().Contains("application/json"))
        {
            // Authenticated but lacks the required role/claim.
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }
        // Razor Pages (Identity): preserve the normal access-denied redirect.
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

// Add email services
EmailService EmailInstance(IServiceProvider i) => new(
    builder.Configuration["EmailSender:Host"],
    builder.Configuration["EmailSender:Mail"],
    builder.Configuration["EmailSender:ApiKey"]
);
builder.Services.AddTransient<IEmailService, EmailService>(EmailInstance);
builder.Services.AddTransient<IEmailSender, EmailService>(EmailInstance);

// Add Stripe
builder.Services.AddScoped<IStripeService, StripeService>(i => new StripeService());
StripeConfiguration.ApiKey = builder.Configuration["StripeConfig:SecretApiKey"];

// Add Github
builder.Services.AddScoped<IIndexService, GithubService>(i =>
    new GithubService(
        builder.Configuration["GitHub:ContentAPI"],
        builder.Configuration["GitHub:Token"]
    )
);

// Add SMS
builder.Services.AddScoped<ISmsSender, SmsSender>(i =>
    new SmsSender(
        builder.Configuration["SmsSender:Host"],
        builder.Configuration["SmsSender:ApiKey"],
        builder.Configuration["SmsSender:From"]
    )
);

// Add Calendar service
builder.Services.AddScoped<IICalService, ICalService>(
    i => new ICalService(builder.Configuration["ICal:ICalAddress"])
);

// Add Problem Details (RFC 7807 API error responses with content negotiation)
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = ctx =>
        ctx.ProblemDetails.Extensions["requestId"] =
            Activity.Current?.Id ?? ctx.HttpContext.TraceIdentifier
);

// Add controllers and views
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();


/* Build application */
var app = builder.Build();


/* Configure: Use this section to configure the HTTP request pipeline. */

var env = app.Environment;

if (!env.IsDevelopment())
{
    app.UseExceptionHandler(exceptionApp => exceptionApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.Headers.CacheControl = "no-cache, no-store";

        // Use content negotiation: API clients (fetch) receive RFC 7807 Problem Details JSON;
        // browser-navigated pages (Identity) receive the static HTML error page.
        var problemDetails = context.RequestServices.GetRequiredService<IProblemDetailsService>();
        var written = await problemDetails.TryWriteAsync(new() { HttpContext = context });

        if (!written)
        {
            context.Response.ContentType = "text/html";
            await context.Response.SendFileAsync(
                env.WebRootFileProvider.GetFileInfo("error.html")
            );
        }
    }));
    app.UseHsts();
}

app.UseHttpsRedirection();
app.MapStaticAssets();

// Serve the React SPA build output from wwwroot/spa
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Razor pages (Identity)
app.MapRazorPages().WithStaticAssets();

// API controllers
app.MapControllers();

// All remaining requests are handled by the React SPA.
// API and Identity routes are matched above; this catches everything else.
app.MapFallbackToFile("spa/index.html");


/* Run the application */
app.Run();
