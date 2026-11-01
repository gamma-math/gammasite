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

    [Theory]
    [InlineData(false, SystemEmailTemplateService.PaymentConfirmation, "MOBILEPAY", "MobilePay")]
    [InlineData(true, SystemEmailTemplateService.PaymentRecurring, "MOBILEPAY", "MobilePay")]
    [InlineData(false, SystemEmailTemplateService.PaymentConfirmation, "STRIPE", "Stripe")]
    public async Task SendPaymentConfirmation_SelectsTemplateAndRendersPaymentDetails(
        bool recurring,
        string templateType,
        string provider,
        string expectedProvider)
    {
        await using var db = CreateDb();
        db.EmailTemplates.Add(new EmailTemplate
        {
            Name = "Payment",
            Subject = "{{ProductName}} - {{PaymentProvider}}",
            HtmlBody = "<p>{{Name}}|{{Email}}|{{ProductName}}|{{Amount}}|{{PaymentProvider}}|{{PaymentDate}}|{{BillingPeriod}}|{{SubscriptionUrl}}</p>",
            TemplateType = templateType,
            IsActive = true,
            Updated = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var email = new Mock<IEmailService>();
        string? subject = null;
        string? html = null;
        email.Setup(value => value.SendEmailAsync("member@example.com", It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string, string>((_, currentSubject, currentHtml) =>
            {
                subject = currentSubject;
                html = currentHtml;
            })
            .Returns(Task.CompletedTask);

        await new SystemEmailTemplateService(db, email.Object)
            .SendPaymentConfirmationAsync(
                "member@example.com",
                "Member",
                "Kontingent",
                15000,
                "DKK",
                provider,
                new DateTime(2026, 10, 20, 10, 30, 0, DateTimeKind.Utc),
                recurring,
                "YEAR",
                1,
                "https://example.com/account");

        Assert.Equal($"Kontingent - {expectedProvider}", subject);
        Assert.NotNull(html);
        Assert.Contains("150,00 kr.", html);
        if (recurring)
        {
            Assert.Contains("rligt", html);
        }
        Assert.Contains("https://example.com/account", html);
    }

    [Fact]
    public async Task SendPaymentConfirmation_SkipsWhenTemplateIsMissingOrInactive()
    {
        await using var db = CreateDb();
        db.EmailTemplates.Add(new EmailTemplate
        {
            Name = "Inactive payment",
            Subject = "Payment",
            HtmlBody = "<p>Payment</p>",
            TemplateType = SystemEmailTemplateService.PaymentConfirmation,
            IsActive = false
        });
        await db.SaveChangesAsync();
        var email = new Mock<IEmailService>();

        await new SystemEmailTemplateService(db, email.Object).SendPaymentConfirmationAsync(
            "member@example.com", "Member", "Kontingent", 10000, "DKK", "STRIPE",
            DateTime.UtcNow, false, null, null, null);

        email.Verify(value => value.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SendEventConfirmation_UsesActiveTemplateAndSkipsWhenMissing()
    {
        await using var db = CreateDb();
        db.EmailTemplates.Add(new EmailTemplate
        {
            Name = "Event confirmation",
            Subject = "{{EventTitle}} - {{RegistrationStatus}}",
            HtmlBody = "<p>{{Heading}}|{{Intro}}|{{Name}}|{{EventTitle}}|{{EventStartDate}}|{{EventUrl}}|{{RegistrationStatus}}</p>",
            TemplateType = SystemEmailTemplateService.EventConfirmation,
            IsActive = true
        });
        await db.SaveChangesAsync();
        var email = new Mock<IEmailService>();
        string? html = null;
        email.Setup(value => value.SendEmailAsync("member@example.com", It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string, string>((_, _, body) => html = body)
            .Returns(Task.CompletedTask);
        var content = new ContentItem
        {
            Title = "Julefrokost",
            Slug = "julefrokost-2026",
            StartDate = new DateTime(2026, 12, 12, 14, 30, 0, DateTimeKind.Utc),
            Type = ContentTypes.Event
        };

        await new SystemEmailTemplateService(db, email.Object).SendEventConfirmationAsync(
            "member@example.com", "Member", content, RegistrationTypes.Waitlisted);

        Assert.Contains("Julefrokost", html);
        Assert.Contains("Du er skrevet p&#xE5; venteliste", html);
        Assert.Contains("Hvis der bliver en ledig plads", html);
        Assert.Contains("Venteliste", html);
        Assert.Contains("/events/julefrokost-2026", html);

        await using var missingDb = CreateDb();
        var missingEmail = new Mock<IEmailService>();
        await new SystemEmailTemplateService(missingDb, missingEmail.Object)
            .SendEventConfirmationAsync("member@example.com", "Member", content, RegistrationTypes.Attendee);
        missingEmail.Verify(value => value.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    private static ApplicationDbContext CreateDb() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
