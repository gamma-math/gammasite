using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using GamMaSite.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using GamMaSite.Services;
using GamMaSite.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using Stripe;

namespace GamMaSite
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var mysqlConn = Configuration.GetConnectionString("DefaultConnection");
            var serverVersion = new MySqlServerVersion(new Version(8, 0));
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseMySql(mysqlConn, serverVersion)
                );
            services.AddDatabaseDeveloperPageExceptionFilter();

            services.AddHsts(options =>
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365);
            });

            services.AddDefaultIdentity<SiteUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddRoles<IdentityRole>()
                .AddRoleManager<RoleManager<IdentityRole>>()
                .AddErrorDescriber<DanishIdentityErrorDescriber>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();
            EmailService EmailInstance(IServiceProvider i) => new(
                Configuration["EmailSender:Host"], 
                Configuration["EmailSender:Mail"], 
                Configuration["EmailSender:ApiKey"]
                );
            services.AddTransient<IEmailService, EmailService>(EmailInstance);
            services.AddTransient<IEmailSender, EmailService>(EmailInstance);
            services.AddScoped<IStripeService, StripeService>(i => new StripeService());
            services.AddScoped<IIndexService, GithubService>(i => 
            new GithubService(
                Configuration["GitHub:ContentAPI"],
                Configuration["GitHub:Token"]
                )
            );
            StripeConfiguration.ApiKey = Configuration["StripeConfig:SecretApiKey"];
            services.AddScoped<ISmsSender, SmsSender>(i =>
            new SmsSender(
                Configuration["SmsSender:Host"],
                Configuration["SmsSender:ApiKey"],
                Configuration["SmsSender:From"]
                )
            );
            services.AddScoped<IICalService, ICalService>(i => new ICalService(Configuration["ICal:ICalAddress"]));
            services.AddControllersWithViews();
            services.AddRazorPages();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }
    }
}
