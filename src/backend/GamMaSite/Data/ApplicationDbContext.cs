using System;
using System.Collections.Generic;
using System.Text;
using GamMaSite.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GamMaSite.Data
{
    public class ApplicationDbContext : IdentityDbContext<SiteUser>
    {
        public DbSet<ContentItem> ContentItems { get; set; }

        public DbSet<ContentLink> ContentLinks { get; set; }

        public DbSet<EventRegistration> EventRegistrations { get; set; }

        public DbSet<EmailTemplate> EmailTemplates { get; set; }

        public ApplicationDbContext() : base()
        {
        }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);


            builder.Entity<IdentityRole>(entity =>
            {
                entity.Property(m => m.Id).HasMaxLength(127);
                entity.Property(m => m.ConcurrencyStamp).HasColumnType("varchar(256)");
            });

            builder.Entity<IdentityUserLogin<string>>(entity =>
            {
                entity.Property(m => m.LoginProvider).HasMaxLength(127);
                entity.Property(m => m.ProviderKey).HasMaxLength(127);
            });

            builder.Entity<IdentityUserRole<string>>(entity =>
            {
                entity.Property(m => m.UserId).HasMaxLength(127);
                entity.Property(m => m.RoleId).HasMaxLength(127);
            });

            builder.Entity<IdentityUserToken<string>>(entity =>
            {
                entity.Property(m => m.UserId).HasMaxLength(127);
                entity.Property(m => m.LoginProvider).HasMaxLength(127);
                entity.Property(m => m.Name).HasMaxLength(127);
            });

            builder.Entity<ContentItem>(entity =>
            {
                entity.HasKey(m => m.Id);
                entity.HasIndex(m => m.Slug).IsUnique().HasDatabaseName("UX_ContentItems_Slug");
                entity.HasIndex(m => m.Type).HasDatabaseName("IX_ContentItems_Type");
                entity.HasIndex(m => m.Status).HasDatabaseName("IX_ContentItems_Status");
                entity.HasIndex(m => m.ShowOnFrontPage).HasDatabaseName("IX_ContentItems_ShowOnFrontPage");
                entity.HasIndex(m => m.StartDate).HasDatabaseName("IX_ContentItems_StartDate");
                entity.HasIndex(m => m.PublishedAt).HasDatabaseName("IX_ContentItems_PublishedAt");
                entity.HasIndex(m => m.CreatedByUserId).HasDatabaseName("IX_ContentItems_CreatedByUserId");

                entity.Property(m => m.Title).HasMaxLength(256).IsRequired();
                entity.Property(m => m.Slug).HasMaxLength(256).IsRequired();
                entity.Property(m => m.Summary).HasMaxLength(512);
                entity.Property(m => m.Body).HasColumnType("longtext");
                entity.Property(m => m.PictureUrl).HasMaxLength(1024);
                entity.Property(m => m.Tags).HasMaxLength(512);
                entity.Property(m => m.Type).HasMaxLength(32).IsRequired();
                entity.Property(m => m.Status).HasMaxLength(32).IsRequired();
                entity.Property(m => m.ShowOnFrontPage).HasDefaultValue(true);
                entity.Property(m => m.Location).HasMaxLength(256);
                entity.Property(m => m.CreatedByUserId).HasMaxLength(128);
                entity.Property(m => m.Created).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(m => m.Updated).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(m => m.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(m => m.CreatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<ContentLink>(entity =>
            {
                entity.HasKey(m => m.Id);
                entity.HasIndex(m => m.ContentItemId).HasDatabaseName("IX_ContentLinks_ContentItemId");

                entity.Property(m => m.Label).HasMaxLength(128).IsRequired();
                entity.Property(m => m.Url).HasMaxLength(1024).IsRequired();
                entity.Property(m => m.Type).HasMaxLength(32).IsRequired();
                entity.Property(m => m.SortOrder).HasDefaultValue(0);
                entity.Property(m => m.Created).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(m => m.Updated).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(m => m.ContentItem)
                    .WithMany(m => m.Links)
                    .HasForeignKey(m => m.ContentItemId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<EventRegistration>(entity =>
            {
                entity.HasKey(m => m.Id);
                entity.HasIndex(m => new { m.ContentItemId, m.UserId })
                    .IsUnique()
                    .HasDatabaseName("UX_EventRegistrations_ContentItemId_UserId");
                entity.HasIndex(m => m.ContentItemId).HasDatabaseName("IX_EventRegistrations_ContentItemId");
                entity.HasIndex(m => m.UserId).HasDatabaseName("IX_EventRegistrations_UserId");

                entity.Property(m => m.UserId).HasMaxLength(128).IsRequired();
                entity.Property(m => m.RegistrationType).HasMaxLength(32).IsRequired();
                entity.Property(m => m.Registered).HasDefaultValue(true);
                entity.Property(m => m.ResponseText).HasMaxLength(1024);
                entity.Property(m => m.Created).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(m => m.Updated).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(m => m.ContentItem)
                    .WithMany(m => m.EventRegistrations)
                    .HasForeignKey(m => m.ContentItemId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.User)
                    .WithMany()
                    .HasForeignKey(m => m.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<EmailTemplate>(entity =>
            {
                entity.HasKey(m => m.Id);
                entity.HasIndex(m => m.TemplateType).HasDatabaseName("IX_EmailTemplates_TemplateType");
                entity.HasIndex(m => m.IsActive).HasDatabaseName("IX_EmailTemplates_IsActive");

                entity.Property(m => m.Name).HasMaxLength(128).IsRequired();
                entity.Property(m => m.Subject).HasMaxLength(256).IsRequired();
                entity.Property(m => m.Preheader).HasMaxLength(256);
                entity.Property(m => m.HtmlBody).HasColumnType("longtext").IsRequired();
                entity.Property(m => m.TextBody).HasColumnType("longtext");
                entity.Property(m => m.TemplateType).HasMaxLength(32).IsRequired();
                entity.Property(m => m.IsActive).HasDefaultValue(true);
                entity.Property(m => m.Created).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(m => m.Updated).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
        }
    }

    public class UserConfiguration : IEntityTypeConfiguration<SiteUser>
    {
        public void Configure(EntityTypeBuilder<SiteUser> builder)
        {
            // This Converter will perform the conversion to and from Json to the desired type
        }
    }
}
