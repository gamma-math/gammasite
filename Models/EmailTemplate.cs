using System;

namespace GamMaSite.Models
{
    public class EmailTemplate
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
}
