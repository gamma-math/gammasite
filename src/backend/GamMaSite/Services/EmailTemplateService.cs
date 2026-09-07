using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GamMaSite.Data;
using GamMaSite.Models;
using GamMaSite.ViewModels.Api;
using Microsoft.EntityFrameworkCore;

namespace GamMaSite.Services
{
    /*
     * Encapsulates reusable email template storage, validation, and rendering.
     */
    public class EmailTemplateService : IEmailTemplateService
    {
        private static readonly Regex PlaceholderRegex = new(@"\{\{([A-Za-z0-9_]+)\}\}", RegexOptions.Compiled);

        private readonly ApplicationDbContext _db;

        public EmailTemplateService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<EmailTemplate>> GetAllAsync(string templateType, bool? isActive)
        {
            var query = _db.EmailTemplates.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(templateType))
            {
                var normalizedType = templateType.Trim().ToUpperInvariant();
                query = query.Where(item => item.TemplateType == normalizedType);
            }

            if (isActive.HasValue)
            {
                query = query.Where(item => item.IsActive == isActive.Value);
            }

            return await query
                .OrderBy(item => item.Name)
                .ToListAsync();
        }

        public async Task<EmailTemplate> GetByIdAsync(int id)
        {
            return await _db.EmailTemplates.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
        }

        public async Task<EmailTemplate> CreateAsync(SaveEmailTemplateRequest request)
        {
            var now = DateTime.UtcNow;
            var template = new EmailTemplate
            {
                Name = Required(request.Name, nameof(request.Name)),
                Subject = Required(request.Subject, nameof(request.Subject)),
                Preheader = request.Preheader,
                HtmlBody = Required(request.HtmlBody, nameof(request.HtmlBody)),
                TextBody = request.TextBody,
                TemplateType = Required(request.TemplateType, nameof(request.TemplateType)).ToUpperInvariant(),
                IsActive = request.IsActive,
                Created = now,
                Updated = now
            };

            _db.EmailTemplates.Add(template);
            await _db.SaveChangesAsync();

            return await GetByIdAsync(template.Id);
        }

        public async Task<EmailTemplate> UpdateAsync(int id, SaveEmailTemplateRequest request)
        {
            var template = await _db.EmailTemplates.FindAsync(id);
            if (template == null)
            {
                return null;
            }

            template.Name = Required(request.Name, nameof(request.Name));
            template.Subject = Required(request.Subject, nameof(request.Subject));
            template.Preheader = request.Preheader;
            template.HtmlBody = Required(request.HtmlBody, nameof(request.HtmlBody));
            template.TextBody = request.TextBody;
            template.TemplateType = Required(request.TemplateType, nameof(request.TemplateType)).ToUpperInvariant();
            template.IsActive = request.IsActive;
            template.Updated = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return await GetByIdAsync(template.Id);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var template = await _db.EmailTemplates.FindAsync(id);
            if (template == null)
            {
                return false;
            }

            _db.EmailTemplates.Remove(template);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<EmailTemplatePreviewDto> PreviewAsync(int id, PreviewEmailTemplateRequest request)
        {
            var template = await GetByIdAsync(id);
            if (template == null)
            {
                return null;
            }

            var values = request?.Values ?? new Dictionary<string, string>();

            return new EmailTemplatePreviewDto
            {
                Subject = Render(template.Subject, values),
                Preheader = Render(template.Preheader, values),
                HtmlBody = Render(template.HtmlBody, values),
                TextBody = Render(template.TextBody, values)
            };
        }

        private static string Render(string template, Dictionary<string, string> values)
        {
            if (template == null)
            {
                return null;
            }

            return PlaceholderRegex.Replace(template, match =>
            {
                var key = match.Groups[1].Value;
                return values.TryGetValue(key, out var value) ? value ?? string.Empty : match.Value;
            });
        }

        private static string Required(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"{name} is required");
            }

            return value.Trim();
        }
    }
}
