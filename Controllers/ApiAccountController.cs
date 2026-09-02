using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using GamMaSite.Models;
using GamMaSite.ViewModels.Api;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;

namespace GamMaSite.Controllers
{
    [ApiController]
    [Route("api/account")]
    [AutoValidateAntiforgeryToken]
    public class ApiAccountController : ControllerBase
    {
        private readonly SignInManager<SiteUser> _signInManager;
        private readonly UserManager<SiteUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IAntiforgery _antiforgery;

        public ApiAccountController(
            SignInManager<SiteUser> signInManager,
            UserManager<SiteUser> userManager,
            IEmailSender emailSender,
            IAntiforgery antiforgery)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _emailSender = emailSender;
            _antiforgery = antiforgery;
        }

        [HttpGet("csrf-token")]
        [AllowAnonymous]
        public IActionResult GetCsrfToken()
        {
            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
            return Ok(new { token = tokens.RequestToken });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var returnUrl = SafeReturnUrl(request.ReturnUrl);
            var result = await _signInManager.PasswordSignInAsync(request.Email, request.Password, request.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                return Ok(new { redirectUrl = returnUrl });
            }

            if (result.RequiresTwoFactor)
            {
                return Ok(new { requiresTwoFactor = true, redirectUrl = $"/Identity/Account/LoginWith2fa?ReturnUrl={Uri.EscapeDataString(returnUrl)}&RememberMe={request.RememberMe}" });
            }

            if (result.IsLockedOut)
            {
                return BadRequest(new { error = "Din konto er låst." });
            }

            return BadRequest(new { error = "Login fejlede. Prøv igen!" });
        }

        [HttpPost("register")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-email")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            var returnUrl = SafeReturnUrl(request.ReturnUrl);
            var user = new SiteUser
            {
                UserName = request.Email,
                Email = request.Email,
                Adresse = request.Adresse,
                Navn = request.Navn,
                PhoneNumber = request.PhoneNumber,
                Aargang = request.Aargang,
                Beskaeftigelse = request.Beskaeftigelse,
                Status = UserStatus.OPRETTET,
                KontingentDato = DateTime.MinValue.ToUniversalTime(),
                OprettetDato = DateTime.UtcNow,
                Visibility = VisibilityStatus.VISIBLE
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                return BadRequest(new { error = string.Join(" ", result.Errors.Select(error => error.Description)) });
            }

            var callbackUrl = await BuildEmailConfirmationUrl(user, returnUrl);
            await _emailSender.SendEmailAsync(request.Email, "Bekræft din email",
                $"Bekræft venligst din GamMa-bruger ved at <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>klikke her</a>.<br /><br />" +
                "For at blive godkendt som medlem, kan du kontakte foreningens bestyrelse på bestyrelsen@gam-ma.dk." +
                "Gør i den forbindelse opmærksom på, om du er studerende eller har færdiggjort dine studier.");

            if (_userManager.Options.SignIn.RequireConfirmedAccount)
            {
                return Ok(new { message = "Din bruger er oprettet. Tjek din email for at bekræfte kontoen.", redirectUrl = string.Empty });
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            return Ok(new { redirectUrl = returnUrl });
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-email")]
        public async Task<IActionResult> ForgotPassword(EmailRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user != null && await _userManager.IsEmailConfirmedAsync(user))
            {
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", code },
                    protocol: Request.Scheme);

                await _emailSender.SendEmailAsync(
                    request.Email,
                    "Nulstil Password",
                    $"Nulstil venligst kodeord ved at <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>klikke her</a>.");
            }

            return Ok(new { message = "Hvis emailen findes hos os, er der sendt et link til nulstilling." });
        }

        [HttpPost("resend-email-confirmation")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-email")]
        public async Task<IActionResult> ResendEmailConfirmation(EmailRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user != null)
            {
                var callbackUrl = await BuildEmailConfirmationUrl(user, "/react");
                await _emailSender.SendEmailAsync(
                    request.Email,
                    "Bekræft din email",
                    $"Bekræft venligst din profil ved at <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>klikke her</a>.");
            }

            return Ok(new { message = "Hvis emailen findes hos os, er bekræftelsesmailen sendt." });
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            return Ok(new
            {
                username = await _userManager.GetUserNameAsync(user),
                email = await _userManager.GetEmailAsync(user),
                isEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user),
                navn = user.Navn,
                adresse = user.Adresse,
                aargang = user.Aargang,
                phoneNumber = await _userManager.GetPhoneNumberAsync(user),
                beskaeftigelse = user.Beskaeftigelse,
                status = user.Status.ToString(),
                visibility = user.Visibility.IsVisible()
            });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok(new { redirectUrl = "/react" });
        }

        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (request.PhoneNumber != phoneNumber)
            {
                var phoneResult = await _userManager.SetPhoneNumberAsync(user, request.PhoneNumber);
                if (!phoneResult.Succeeded)
                {
                    return BadRequest(new { error = "En uventet fejl opstod, da telefonnummer blev forsøgt ændret." });
                }
            }

            user.Navn = request.Navn;
            user.Adresse = request.Adresse;
            user.Aargang = request.Aargang;
            user.Beskaeftigelse = request.Beskaeftigelse;
            user.Visibility = request.Visibility.ToVisibility();

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(new { error = string.Join(" ", result.Errors.Select(error => error.Description)) });
            }

            await _signInManager.RefreshSignInAsync(user);
            return Ok(new { message = "Din profil er blevet opdateret" });
        }

        [HttpPost("change-email")]
        [Authorize]
        [EnableRateLimiting("auth-email")]
        public async Task<IActionResult> ChangeEmail(ChangeEmailRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var email = await _userManager.GetEmailAsync(user);
            if (request.NewEmail == email)
            {
                return Ok(new { message = "Din email er uforandret." });
            }

            var userId = await _userManager.GetUserIdAsync(user);
            var code = await _userManager.GenerateChangeEmailTokenAsync(user, request.NewEmail);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmailChange",
                pageHandler: null,
                values: new { area = "Identity", userId, email = request.NewEmail, code },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(
                request.NewEmail,
                "Bekræft email",
                $"Bekræft venligst din GamMa-bruger ved at <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>klikke her</a>.");

            return Ok(new { message = "Bekræftelseslink er blevet sendt til din email. Tjek venligst din email." });
        }

        [HttpPost("send-verification-email")]
        [Authorize]
        [EnableRateLimiting("auth-email")]
        public async Task<IActionResult> SendVerificationEmail()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var email = await _userManager.GetEmailAsync(user);
            var callbackUrl = await BuildEmailConfirmationUrl(user, "/react/account/manage/email");
            await _emailSender.SendEmailAsync(
                email,
                "Bekræft din email",
                $"Bekræft venligst din profil ved at <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>klikke her</a>.");

            return Ok(new { message = "Bekræftelsesmail afsendt. Tjek venligst din mail." });
        }

        [HttpPost("change-password")]
        [Authorize]
        [EnableRateLimiting("account-sensitive")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var hasPassword = await _userManager.HasPasswordAsync(user);
            if (!hasPassword)
            {
                return BadRequest(new { error = "Denne bruger har ikke et lokalt password endnu." });
            }

            var result = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                return BadRequest(new { error = string.Join(" ", result.Errors.Select(error => error.Description)) });
            }

            await _signInManager.RefreshSignInAsync(user);
            return Ok(new { message = "Dit password er blevet ændret." });
        }

        [HttpGet("personal-data")]
        [Authorize]
        public async Task<IActionResult> DownloadPersonalData()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var personalData = new Dictionary<string, string>();
            var personalDataProps = typeof(SiteUser).GetProperties().Where(
                prop => Attribute.IsDefined(prop, typeof(PersonalDataAttribute)));

            foreach (var property in personalDataProps)
            {
                personalData.Add(property.Name, property.GetValue(user)?.ToString() ?? "null");
            }

            var logins = await _userManager.GetLoginsAsync(user);
            foreach (var login in logins)
            {
                personalData.Add($"{login.LoginProvider} external login provider key", login.ProviderKey);
            }

            return File(JsonSerializer.SerializeToUtf8Bytes(personalData), "application/json", "PersonalData.json");
        }

        [HttpPost("delete")]
        [Authorize]
        [EnableRateLimiting("account-sensitive")]
        public async Task<IActionResult> DeleteAccount(DeleteAccountRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var hasPassword = await _userManager.HasPasswordAsync(user);
            if (hasPassword && !await _userManager.CheckPasswordAsync(user, request.Password ?? string.Empty))
            {
                return BadRequest(new { error = "Password er ikke korrekt." });
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(new { error = string.Join(" ", result.Errors.Select(error => error.Description)) });
            }

            await _signInManager.SignOutAsync();
            return Ok(new { redirectUrl = "/react" });
        }

        private async Task<string> BuildEmailConfirmationUrl(SiteUser user, string returnUrl)
        {
            var userId = await _userManager.GetUserIdAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            return Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId, code, returnUrl = SafeReturnUrl(returnUrl) },
                protocol: Request.Scheme);
        }

        private static string SafeReturnUrl(string returnUrl)
        {
            if (string.IsNullOrWhiteSpace(returnUrl) || !returnUrl.StartsWith("/") || returnUrl.StartsWith("//"))
            {
                return "/react";
            }

            if (returnUrl.StartsWith("/Identity/Account/Login", StringComparison.OrdinalIgnoreCase)
                || returnUrl.StartsWith("/Identity/Account/Register", StringComparison.OrdinalIgnoreCase)
                || returnUrl.StartsWith("/react/account/login", StringComparison.OrdinalIgnoreCase)
                || returnUrl.StartsWith("/react/account/register", StringComparison.OrdinalIgnoreCase))
            {
                return "/react";
            }

            if (returnUrl.StartsWith("/Identity/Account/Manage/Email", StringComparison.OrdinalIgnoreCase))
            {
                return "/react/account/manage/email";
            }

            if (returnUrl.StartsWith("/Identity/Account/Manage/ChangePassword", StringComparison.OrdinalIgnoreCase))
            {
                return "/react/account/manage/password";
            }

            if (returnUrl.StartsWith("/Identity/Account/Manage", StringComparison.OrdinalIgnoreCase))
            {
                return "/react/account/manage";
            }

            return returnUrl;
        }
    }
}
