using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GamMaSite.Data;
using GamMaSite.Models;
using GamMaSite.Services;
using GamMaSite.ViewModels.Api;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GamMaSite.Tests;

/*
 * Tests the email template service used by the React admin template APIs.
 * Covers template filtering and placeholder rendering for previews.
 */
public class EmailTemplateServiceTests
{
    [Fact]
    public async Task PreviewAsync_RendersKnownPlaceholdersAndPreservesUnknownOnes()
    {
        await using var db = CreateDb();
        db.EmailTemplates.Add(new EmailTemplate
        {
            Id = 1, Name = "Template", Subject = "Hello {{Name}}", HtmlBody = "<p>{{Name}} {{Unknown}}</p>",
            TemplateType = "EVENT", IsActive = true
        });
        await db.SaveChangesAsync();

        var preview = await new EmailTemplateService(db).PreviewAsync(1,
            new PreviewEmailTemplateRequest { Values = new Dictionary<string, string> { ["Name"] = "Ada" } });

        Assert.Equal("Hello Ada", preview.Subject);
        Assert.Equal("<p>Ada {{Unknown}}</p>", preview.HtmlBody);
    }

    [Fact]
    public async Task GetAllAsync_FiltersTypeAndActiveState()
    {
        await using var db = CreateDb();
        db.EmailTemplates.AddRange(
            new EmailTemplate { Name = "B", Subject = "B", HtmlBody = "B", TemplateType = "EVENT", IsActive = true },
            new EmailTemplate { Name = "A", Subject = "A", HtmlBody = "A", TemplateType = "SYSTEM", IsActive = true },
            new EmailTemplate { Name = "C", Subject = "C", HtmlBody = "C", TemplateType = "EVENT", IsActive = false });
        await db.SaveChangesAsync();

        var result = await new EmailTemplateService(db).GetAllAsync("event", true);

        var template = Assert.Single(result);
        Assert.Equal("B", template.Name);
    }

    private static ApplicationDbContext CreateDb() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
