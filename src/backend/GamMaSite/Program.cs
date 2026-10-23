using System;
using System.Data;
using System.IO;
using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using Stripe;
using GamMaSite.Configuration;
using GamMaSite.Data;
using GamMaSite.Models;
using GamMaSite.Services;


LocalEnvironment.Load();

/* Create WebApplicationBuilder */
var builder = WebApplication.CreateBuilder(args);

var publicBaseUrl = builder.Configuration["PublicBaseUrl"]?.TrimEnd('/');
if (!Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out var publicBaseUri) ||
    (publicBaseUri.Scheme != Uri.UriSchemeHttps && publicBaseUri.Scheme != Uri.UriSchemeHttp))
{
    throw new InvalidOperationException("PublicBaseUrl skal være en fuld HTTP(S)-URL.");
}


/* ConfigureServices: Use this section to add services to the container. */
// For more: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/startup?view=aspnetcore-8.0

// Add the database connection
var mysqlConn = builder.Configuration.GetConnectionString("DefaultConnection");
var serverVersion = new MySqlServerVersion(new Version(8, 0));
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        mysqlConn,
        serverVersion,
        mySqlOptions => mySqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
    )
);
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
    options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    }
)
.AddRoles<IdentityRole>()
.AddRoleManager<RoleManager<IdentityRole>>()
.AddErrorDescriber<DanishIdentityErrorDescriber>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

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

// Add MobilePay/Vipps ePayment integration.
builder.Services.AddHttpClient<IMobilePayService, MobilePayService>(client =>
{
    var baseUrl = builder.Configuration["MobilePay:BaseUrl"];
    if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var mobilePayBaseUri) ||
        (mobilePayBaseUri.Scheme != Uri.UriSchemeHttps && mobilePayBaseUri.Scheme != Uri.UriSchemeHttp))
    {
        throw new InvalidOperationException("MobilePay:BaseUrl skal være en fuld HTTP(S)-URL.");
    }

    client.BaseAddress = new Uri(mobilePayBaseUri.ToString().TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(20);
});
builder.Services.AddScoped<IMobilePayEventReferenceService, MobilePayEventReferenceService>();
builder.Services.AddScoped<MobilePaySubscriptionReconciliationService>();

// API services used by the future React frontend.
builder.Services.AddScoped<IContentService, ContentService>();
builder.Services.AddScoped<IContentMediaService, ContentMediaService>();
builder.Services.AddScoped<IEventRegistrationService, EventRegistrationService>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddScoped<ISystemEmailTemplateService, SystemEmailTemplateService>();
builder.Services.AddScoped<FinanceReportService>();
builder.Services.AddScoped<FinanceImportService>();
builder.Services.AddScoped<IAccessControlService, AccessControlService>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PermissionPolicies.ContentEdit, policy => policy.RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(PermissionCodes.ContentEdit)));
    options.AddPolicy(PermissionPolicies.ContentOrMessages, policy => policy.RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(PermissionCodes.ContentEdit, PermissionCodes.MessagesEdit)));
    options.AddPolicy(PermissionPolicies.ContentOrRegistrations, policy => policy.RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(PermissionCodes.ContentEdit, PermissionCodes.RegistrationsEdit)));
    options.AddPolicy(PermissionPolicies.ContentOrEmailTemplates, policy => policy.RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(PermissionCodes.ContentEdit, PermissionCodes.EmailTemplatesEdit)));
    options.AddPolicy(PermissionPolicies.EmailTemplatesOrMessages, policy => policy.RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(PermissionCodes.EmailTemplatesEdit, PermissionCodes.MessagesEdit)));
    options.AddPolicy(PermissionPolicies.ContentOrEmailTemplatesOrMessages, policy => policy.RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(PermissionCodes.ContentEdit, PermissionCodes.EmailTemplatesEdit, PermissionCodes.MessagesEdit)));
    options.AddPolicy(PermissionPolicies.MemberData, policy => policy.RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(PermissionCodes.ContentEdit, PermissionCodes.RegistrationsEdit, PermissionCodes.RolesEdit, PermissionCodes.MessagesEdit, PermissionCodes.FinanceViewAll, PermissionCodes.FinanceEditAll)));
    options.AddPolicy(PermissionPolicies.FinanceMemberData, policy => policy.RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(PermissionCodes.FinanceViewAll, PermissionCodes.FinanceEditAll)));
    options.AddPolicy(PermissionPolicies.RegistrationsEdit, policy => policy.RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(PermissionCodes.RegistrationsEdit)));
    options.AddPolicy(PermissionPolicies.EmailTemplatesEdit, policy => policy.RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(PermissionCodes.EmailTemplatesEdit)));
    options.AddPolicy(PermissionPolicies.MessagesEdit, policy => policy.RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(PermissionCodes.MessagesEdit)));
    options.AddPolicy(PermissionPolicies.RolesEdit, policy => policy.RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(PermissionCodes.RolesEdit)));
    options.AddPolicy(PermissionPolicies.RolesOrMessages, policy => policy.RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(PermissionCodes.RolesEdit, PermissionCodes.MessagesEdit)));
    options.AddPolicy(PermissionPolicies.FinanceViewAll, policy => policy.RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(PermissionCodes.FinanceViewAll, PermissionCodes.FinanceEditAll)));
    options.AddPolicy(PermissionPolicies.FinanceEditAll, policy => policy.RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(PermissionCodes.FinanceEditAll)));
    options.AddPolicy(PermissionPolicies.PaymentsEditAll, policy => policy.RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(PermissionCodes.PaymentsEditAll)));
});

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

// Add controllers and views
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("auth-login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy("auth-email", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0
            }));

    options.AddPolicy("account-sensitive", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 8,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0
            }));

    options.AddPolicy("payment-checkout", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            $"{context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous"}:" +
            (context.Connection.RemoteIpAddress?.ToString() ?? "unknown"),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 6,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy("payment-provider-read", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            $"{context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous"}:" +
            (context.Connection.RemoteIpAddress?.ToString() ?? "unknown"),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    foreach (var proxy in builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").GetChildren())
    {
        if (IPAddress.TryParse(proxy.Value, out var address))
        {
            options.KnownProxies.Add(address);
        }
    }
});


/* Build application */
var app = builder.Build();


/* Configure: Use this section to configure the HTTP request pipeline. */

var env = app.Environment;

if (env.IsDevelopment())
{
    await EnsureContentItemsShowOnFrontPageColumnAsync(app.Services);
}

if (!env.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseStaticFiles();
var contentMediaRoot = ContentMediaService.ResolveRootPath(app.Configuration, env.ContentRootPath);
if (Directory.Exists(contentMediaRoot))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(contentMediaRoot),
        RequestPath = ContentMediaService.PublicPath
    });
}
app.MapStaticAssets();

// Redirect the exact lowercase library URL before ASP.NET's case-insensitive
// endpoint matching can treat it as the canonical /Library route.
app.Use(async (context, next) =>
{
    if (HttpMethods.IsGet(context.Request.Method)
        && LegacyRouteRedirects.IsNonCanonicalLibraryPath(context.Request.Path.Value ?? string.Empty))
    {
        context.Response.Redirect(
            LegacyRouteRedirects.BuildLocation("library", context.Request.QueryString.ToString()),
            permanent: true);
        return;
    }

    await next();
});

app.UseRouting();

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

// Redirect legacy React-prefixed public URLs to their canonical paths.
app.MapGet("/react", (HttpContext context) => RedirectLegacyReactPath(string.Empty, context));
app.MapGet("/react/{**path}", (string path, HttpContext context) => RedirectLegacyReactPath(path, context));

// Keep the English /Library route canonical for the React library page.
app.MapGet("/bibliotek", (HttpContext context) => RedirectLegacyReactPath("bibliotek", context));
app.MapGet("/referater", (HttpContext context) => RedirectLegacyReactPath("referater", context));
app.MapGet("/Library/Index", (HttpContext context) => RedirectLegacyReactPath("Library/Index", context));
app.MapGet("/Library/IndexAsync", (HttpContext context) => RedirectLegacyReactPath("Library/IndexAsync", context));

// Controller routes and Razor pages
app.MapControllers();
app.MapMethods(
    "/api/{**path}",
    new[] { HttpMethods.Get, HttpMethods.Head, HttpMethods.Post, HttpMethods.Put, HttpMethods.Patch, HttpMethods.Delete, HttpMethods.Options },
    () => Results.NotFound());
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}").WithStaticAssets();
app.MapFallbackToController("{*path:nonfile}", "Index", "ReactShell");
app.MapGet("/Identity/Account/Login", (HttpContext context) => Results.Redirect($"/account/login{context.Request.QueryString}"));
app.MapGet("/Identity/Account/Register", (HttpContext context) => Results.Redirect($"/account/register{context.Request.QueryString}"));
app.MapGet("/Identity/Account/ForgotPassword", () => Results.Redirect("/account/forgot-password"));
app.MapGet("/Identity/Account/ResendEmailConfirmation", () => Results.Redirect("/account/resend-email-confirmation"));
app.MapGet("/Identity/Account/Manage", () => Results.Redirect("/account/manage"));
app.MapGet("/Identity/Account/Manage/Index", () => Results.Redirect("/account/manage"));
app.MapGet("/Identity/Account/Manage/Email", () => Results.Redirect("/account/manage/email"));
app.MapGet("/Identity/Account/Manage/ChangePassword", () => Results.Redirect("/account/manage/password"));
app.MapGet("/Identity/Account/Manage/TwoFactorAuthentication", () => Results.Redirect("/account/manage/two-factor"));
app.MapGet("/Identity/Account/Manage/GenerateRecoveryCodes", () => Results.Redirect("/account/manage/two-factor"));
app.MapGet("/Identity/Account/Manage/ShowRecoveryCodes", () => Results.Redirect("/account/manage/two-factor"));
app.MapGet("/Identity/Account/Manage/PersonalData", () => Results.Redirect("/account/manage/personal-data"));
app.MapGet("/Identity/Account/Manage/DeletePersonalData", () => Results.Redirect("/account/manage/delete-personal-data"));
app.MapGet("/Identity/Account/Logout", () => Results.Redirect("/account/manage/logout"));
app.MapRazorPages().WithStaticAssets();


/* Run the application */
app.Run();

static IResult RedirectLegacyReactPath(string path, HttpContext context)
{
    return Results.Redirect(
        LegacyRouteRedirects.BuildLocation(path, context.Request.QueryString.ToString()),
        permanent: true);
}

static async Task EnsureContentItemsShowOnFrontPageColumnAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var connection = db.Database.GetDbConnection();
    var shouldClose = connection.State != ConnectionState.Open;

    if (shouldClose)
    {
        await connection.OpenAsync();
    }

    try
    {
        if (!await SchemaValueExistsAsync(connection,
                "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ContentItems' AND COLUMN_NAME = 'ShowOnFrontPage'"))
        {
            await ExecuteSchemaCommandAsync(connection, "ALTER TABLE `ContentItems` ADD COLUMN `ShowOnFrontPage` tinyint(1) NOT NULL DEFAULT 1 AFTER `Status`");
        }

        if (!await SchemaValueExistsAsync(connection,
                "SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ContentItems' AND INDEX_NAME = 'IX_ContentItems_ShowOnFrontPage'"))
        {
            await ExecuteSchemaCommandAsync(connection, "CREATE INDEX `IX_ContentItems_ShowOnFrontPage` ON `ContentItems` (`ShowOnFrontPage`)");
        }

        if (!await SchemaValueExistsAsync(connection,
                "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ContentItems' AND COLUMN_NAME = 'MaxParticipants'"))
        {
            await ExecuteSchemaCommandAsync(connection, "ALTER TABLE `ContentItems` ADD COLUMN `MaxParticipants` int NULL DEFAULT NULL AFTER `Status`");
        }
    }
    finally
    {
        if (shouldClose)
        {
            await connection.CloseAsync();
        }
    }
}

static async Task<bool> SchemaValueExistsAsync(System.Data.Common.DbConnection connection, string sql)
{
    await using var command = connection.CreateCommand();
    command.CommandText = sql;
    var result = await command.ExecuteScalarAsync();
    return Convert.ToInt32(result) > 0;
}

static async Task ExecuteSchemaCommandAsync(System.Data.Common.DbConnection connection, string sql)
{
    await using var command = connection.CreateCommand();
    command.CommandText = sql;
    await command.ExecuteNonQueryAsync();
}

public partial class Program
{
}
