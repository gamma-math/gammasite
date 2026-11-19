using System.ComponentModel.DataAnnotations;

namespace GamMaSite.ViewModels.Api
{
    public class ContactRequestDto
    {
        [Required, EmailAddress, StringLength(254)]
        public string Email { get; set; }

        [Required, RegularExpression("^(Arrangement|Betaling og refusion|Andet)$")]
        public string ContactType { get; set; }

        [Required, StringLength(200)]
        public string Topic { get; set; }

        [Required, StringLength(5000)]
        public string Message { get; set; }
    }
}
