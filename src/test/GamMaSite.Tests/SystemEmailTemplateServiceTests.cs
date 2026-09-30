using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GamMaSite.Data;
using GamMaSite.Models;
using GamMaSite.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace GamMaSite.Tests;

/*
 * Tests system-owned account email rendering.
 * Covers active template selection, placeholder replacement, HTML escaping, and fallback emails.
 */
public class SystemEmailTemplateServiceTests
{
    [Fact]
    public async Task SendEmailConfirmation_UsesActiveTemplateAndEscapesHtmlValues()
    {
        await using var db = CreateDb();
        db.EmailTemplates.Add(new EmailTemplate
        {
            Name = "Confirmation", Subject = "Hello\n{{Name}}", HtmlBody = "<p>{{Name}} {{ConfirmationUrl}}</p>",
            TemplateType = SystemEmailTemplateService.EmailConfirmation, IsActive = true,
            Updated = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var email = new Mock<IEmailService>();
        string? subject = null;
        string? html = null;
        email.Setup(value => value.SendEmailAsync("member@example.com", It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string, string>((_, currentSubject, currentHtml) => { subject = currentSubject; html = currentHtml; })
            .Returns(Task.CompletedTask);

        await new SystemEmailTemplateService(db, email.Object)
            .SendEmailConfirmationAsync("member@example.com", "<Member>", "https://example.com/confirm");

        Assert.Equal("Hello<Member>", subject);
        Assert.Contains("&lt;Member&gt;", html!);
        Assert.Contains("https://example.com/confirm", html!);
    }

    [Fact]
    public async Task SendPasswordReset_UsesFallbackWhenNoTemplateExists()
    {
        await using var db = CreateDb();
        var email = new Mock<IEmailService>();
        string? html = null;
        email.Setup(value => value.SendEmailAsync("member@example.com", It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string, string>((_, _, currentHtml) => html = currentHtml)
            .Returns(Task.CompletedTask);

        await new SystemEmailTemplateService(db, email.Object)
            .SendPasswordResetAsync("member@example.com", "Member", "https://example.com/reset");

        Assert.Contains("Nulstil dit password", html!);
        Assert.Contains("https://example.com/reset", html!);
    }

    private static ApplicationDbContext CreateDb() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
