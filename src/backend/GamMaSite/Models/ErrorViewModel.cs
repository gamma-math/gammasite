using System;

namespace GamMaSite.Models
{
    /*
     * View model for ASP.NET error pages.
     */
    public class ErrorViewModel
    {
        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
