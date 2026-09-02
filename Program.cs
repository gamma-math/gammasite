using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

// API services used by the future React frontend.
builder.Services.AddScoped<IContentService, ContentService>();
builder.Services.AddScoped<IEventRegistrationService, EventRegistrationService>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();

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


/* Build application */
var app = builder.Build();


/* Configure: Use this section to configure the HTTP request pipeline. */

var env = app.Environment;

if (!env.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.MapStaticAssets();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Controller routes and Razor pages
app.MapControllers();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}").WithStaticAssets();
app.MapGet("/Identity/Account/Login", (HttpContext context) => Results.Redirect($"/react/account/login{context.Request.QueryString}"));
app.MapGet("/Identity/Account/Register", (HttpContext context) => Results.Redirect($"/react/account/register{context.Request.QueryString}"));
app.MapGet("/Identity/Account/ForgotPassword", () => Results.Redirect("/react/account/forgot-password"));
app.MapGet("/Identity/Account/ResendEmailConfirmation", () => Results.Redirect("/react/account/resend-email-confirmation"));
app.MapGet("/Identity/Account/Manage", () => Results.Redirect("/react/account/manage"));
app.MapGet("/Identity/Account/Manage/Index", () => Results.Redirect("/react/account/manage"));
app.MapGet("/Identity/Account/Manage/Email", () => Results.Redirect("/react/account/manage/email"));
app.MapGet("/Identity/Account/Manage/ChangePassword", () => Results.Redirect("/react/account/manage/password"));
app.MapGet("/Identity/Account/Manage/TwoFactorAuthentication", () => Results.Redirect("/react/account/manage/two-factor"));
app.MapGet("/Identity/Account/Manage/GenerateRecoveryCodes", () => Results.Redirect("/react/account/manage/two-factor"));
app.MapGet("/Identity/Account/Manage/ShowRecoveryCodes", () => Results.Redirect("/react/account/manage/two-factor"));
app.MapGet("/Identity/Account/Manage/PersonalData", () => Results.Redirect("/react/account/manage/personal-data"));
app.MapGet("/Identity/Account/Manage/DeletePersonalData", () => Results.Redirect("/react/account/manage/delete-personal-data"));
app.MapGet("/Identity/Account/Logout", () => Results.Redirect("/react/account/manage/logout"));
app.MapRazorPages().WithStaticAssets();


/* Run the application */
app.Run();
