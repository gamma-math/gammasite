using System.ComponentModel.DataAnnotations;

namespace GamMaSite.ViewModels.Api
{
    public sealed class LoginRequest
    {
        [Required(ErrorMessage = "Email er obligatorisk")]
        [EmailAddress(ErrorMessage = "Email er ikke gyldig")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password er obligatorisk")]
        public string Password { get; set; }

        public bool RememberMe { get; set; }

        public string ReturnUrl { get; set; }
    }

    public sealed class RegisterRequest
    {
        [Required(ErrorMessage = "Navn er obligatorisk")]
        public string Navn { get; set; }

        [Required(ErrorMessage = "Email er obligatorisk")]
        [EmailAddress(ErrorMessage = "Email er ikke gyldig")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password er obligatorisk")]
        [StringLength(100, ErrorMessage = "Password skal bestå af mindst {2} og højst {1} tegn.", MinimumLength = 6)]
        public string Password { get; set; }

        [Compare("Password", ErrorMessage = "Password og det bekræftede password stemmer ikke overens.")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Telefonnummer er obligatorisk")]
        [Phone(ErrorMessage = "Telefonnummer er ikke gyldigt")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Årgang er obligatorisk")]
        [Range(1849, 2026, ErrorMessage = "Årstallet Årgang skal være mellem {1} og {2}.")]
        public int Aargang { get; set; }

        [Required(ErrorMessage = "Beskæftigelse ved arbejdsgiver er obligatorisk")]
        public string Beskaeftigelse { get; set; }

        [Required(ErrorMessage = "Adresse er obligatorisk")]
        public string Adresse { get; set; }

        public string ReturnUrl { get; set; }
    }

    public sealed class EmailRequest
    {
        [Required(ErrorMessage = "Email er obligatorisk")]
        [EmailAddress(ErrorMessage = "Email er ikke gyldig")]
        public string Email { get; set; }
    }

    public sealed class UpdateProfileRequest
    {
        public string Navn { get; set; }

        public string Adresse { get; set; }

        [Range(1849, 2026, ErrorMessage = "Årstallet Årgang skal være mellem {1} og {2}.")]
        public int Aargang { get; set; }

        [Phone(ErrorMessage = "Telefonnummer er ikke gyldigt")]
        public string PhoneNumber { get; set; }

        public string Beskaeftigelse { get; set; }

        public bool Visibility { get; set; }

        public bool IsStudent { get; set; }
    }

    public sealed class ChangeEmailRequest
    {
        [Required(ErrorMessage = "Ny email er obligatorisk")]
        [EmailAddress(ErrorMessage = "Email er ikke gyldig")]
        public string NewEmail { get; set; }
    }

    public sealed class ChangePasswordRequest
    {
        [Required(ErrorMessage = "Nuværende password er obligatorisk")]
        public string OldPassword { get; set; }

        [Required(ErrorMessage = "Nyt password er obligatorisk")]
        [StringLength(100, ErrorMessage = "Nyt password skal bestå af mindst {2} og højst {1} tegn.", MinimumLength = 6)]
        public string NewPassword { get; set; }

        [Compare("NewPassword", ErrorMessage = "Det nye password stemmer ikke overens med bekræftelsesfeltet.")]
        public string ConfirmPassword { get; set; }
    }

    public sealed class DeleteAccountRequest
    {
        public string Password { get; set; }
    }
}
