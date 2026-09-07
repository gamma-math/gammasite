using System;
using System.Collections.Generic;

namespace GamMaSite.ViewModels.Api
{
    /*
     * DTOs for reusable email template editing and preview rendering.
     */
    public class EmailTemplateDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Subject { get; set; }

        public string Preheader { get; set; }

        public string HtmlBody { get; set; }

        public string TextBody { get; set; }

        public string TemplateType { get; set; }

        public bool IsActive { get; set; }

        public DateTime Created { get; set; }

        public DateTime Updated { get; set; }
    }

    public class SaveEmailTemplateRequest
    {
        public string Name { get; set; }

        public string Subject { get; set; }

        public string Preheader { get; set; }

        public string HtmlBody { get; set; }

        public string TextBody { get; set; }

        public string TemplateType { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class PreviewEmailTemplateRequest
    {
        public Dictionary<string, string> Values { get; set; } = new Dictionary<string, string>();
    }

    public class EmailTemplatePreviewDto
    {
        public string Subject { get; set; }

        public string Preheader { get; set; }

        public string HtmlBody { get; set; }

        public string TextBody { get; set; }
    }
}
