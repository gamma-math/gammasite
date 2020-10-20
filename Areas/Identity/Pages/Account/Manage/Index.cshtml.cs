using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using GamMaSite.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GamMaSite.Areas.Identity.Pages.Account.Manage
{
    public partial class IndexModel : PageModel
    {
        private readonly UserManager<GamMaUser> _userManager;
        private readonly SignInManager<GamMaUser> _signInManager;

        public IndexModel(
            UserManager<GamMaUser> userManager,
            SignInManager<GamMaUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public string Username { get; set; }

        public UserStatus Status { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [DataType(DataType.Text)]
            [Display(Name = "Navn")]
            public string Navn { get; set; }

            [DataType(DataType.Text)]
            [Display(Name = "Adresse")]
            public string Adresse { get; set; }

            [Range(1849, 2025, ErrorMessage = "Årstallet {0} skal være mellem {1} og {2}.")]
            [Display(Name = "Årgang")]
            public int Aargang { get; set; }

            [Phone]
            [Display(Name = "Telefon")]
            public string PhoneNumber { get; set; }

            [DataType(DataType.Text)]
            [Display(Name = "Beskæftigelse")]
            public string Beskaeftigelse { get; set; }

        }

        private async Task LoadAsync(GamMaUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            Username = userName;
            Status = user.Status;

            Input = new InputModel
            {
                Navn = user.Navn,
                Adresse = user.Adresse,
                Aargang = user.Aargang,
                PhoneNumber = phoneNumber,
                Beskaeftigelse = user.Beskaeftigelse
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Ude af stand til at indlæse bruger med ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Ude af stand til at indlæse bruger med ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "En uventet fejl opstod, da telefonnummer blev forsøgt ændret.";
                    return RedirectToPage();
                }
            }

            if (Input.Navn != user.Navn)
            {
                user.Navn = Input.Navn;
            }

            if (Input.Adresse != user.Adresse)
            {
                user.Adresse = Input.Adresse;
            }

            if (Input.Aargang != user.Aargang)
            {
                user.Aargang = Input.Aargang;
            }

            if (Input.Beskaeftigelse != user.Beskaeftigelse)
            {
                user.Beskaeftigelse = Input.Beskaeftigelse;
            }

            await _userManager.UpdateAsync(user);

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Din profil er blevet opdateret";
            return RedirectToPage();
        }
    }
}
