using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using GamMaSite.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace GamMaSite.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<GamMaUser> _signInManager;
        private readonly UserManager<GamMaUser> _userManager;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;

        public RegisterModel(
            UserManager<GamMaUser> userManager,
            SignInManager<GamMaUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "{0} er obligatorisk")]
            [DataType(DataType.Text)]
            [Display(Name = "Navn")]
            public string Navn { get; set; }

            [Required(ErrorMessage = "{0} er obligatorisk")]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required(ErrorMessage = "{0} er obligatorisk")]
            [StringLength(100, ErrorMessage = "{0} skal bestå af mindst {2} og højst {1} tegn.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Bekræft password")]
            [Compare("Password", ErrorMessage = "Password og det bekræftede password stemmer ikke overens.")]
            public string ConfirmPassword { get; set; }

            [Required(ErrorMessage = "{0} er obligatorisk")]
            [Phone]
            [Display(Name = "Telefonnummer")]
            public string PhoneNumber { get; set; }

            [Required(ErrorMessage = "Årgang er obligatorisk")]
            [Range(1849, 2025, ErrorMessage = "Årstallet {0} skal være mellem {1} og {2}.")]
            [Display(Name = "Årgang (start på studiet)")]
            public int Aargang { get; set; }

            [Required(ErrorMessage = "{0} er obligatorisk")]
            [DataType(DataType.Text)]
            [Display(Name = "Beskæftigelse ved arbejdsgiver")]
            public string Beskaeftigelse { get; set; }

            [Required(ErrorMessage = "{0} er obligatorisk")]
            [DataType(DataType.Text)]
            [Display(Name = "Adresse")]
            public string Adresse { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            if (ModelState.IsValid)
            {
                var user = new GamMaUser 
                { 
                    UserName = Input.Email, 
                    Email = Input.Email,
                    Adresse = Input.Adresse,
                    Navn = Input.Navn,
                    PhoneNumber = Input.PhoneNumber,
                    Aargang = Input.Aargang,
                    Beskaeftigelse = Input.Beskaeftigelse,
                    Status = UserStatus.OPRETTET,
                    KontingentDato = DateTime.MinValue,
                    OprettetDato = DateTime.Now
                };
                var result = await _userManager.CreateAsync(user, Input.Password);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Bruger oprettede en ny profil med et password.");

                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = user.Id, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(Input.Email, "Bekræft din email",
                        $"Bekræft venligst din GamMa-bruger ved at <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>klikke her</a>.<br /><br />" +
                        $"Kontakt dernæst bestyrelsen@gam-ma.dk for yderligere instrukser.");

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}
