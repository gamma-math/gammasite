using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GamMaSite.Models;
using GamMaSite.Services;
using GamMaSite.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GamMaSite.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private UserManager<GamMaUser> _userManager;

        private IStripeService _stripeService;

        public PaymentController(UserManager<GamMaUser> userManager, IStripeService stripeService)
        {
            this._userManager = userManager;
            this._stripeService = stripeService;
        }

        public async Task<IActionResult> IndexAsync()
        {
            return View(await _stripeService.GetAllProductsAsync());
        }

        public async Task<IActionResult> ProductAsync(string id)
        {
            var product = await _stripeService.GetProductAsync(id);
            var price = await _stripeService.GetPriceAsync(id);
            return View(new ProductInfo(product, price));
        }

        public async Task<IActionResult> ForAsync(string id)
        {
            var product = await _stripeService.GetProductByNameAsync(id);
            if (product != null)
            {
                return RedirectToAction("Product", new { id = product.Id });
            }
            else
            {
                return RedirectToAction("Index");
            }
        }

        public IActionResult Generisk()
        {
            return View();
        }

        public IActionResult Success()
        {
            return View();
        }

        public async Task<IActionResult> KontingentSuccessAsync(string session)
        {
            var stripeSession = await _stripeService.GetSessionAsync(session);
            var kontingentProduct = await _stripeService.GetProductByNameAsync("kontingent");
            var hasPayed = _stripeService.IsPaymentComplete(stripeSession);
            var correctProduct = stripeSession.Metadata?["Product"] == kontingentProduct.Id;
            var user = await _userManager.FindByIdAsync(stripeSession.ClientReferenceId);
            var sessionCreated = DateTime.Parse(stripeSession.Metadata["SessionCreated"]);
            if (DateTime.Now < sessionCreated.AddHours(1) && hasPayed && correctProduct)
            {
                user.MarkAsPayed();
                await _userManager.UpdateAsync(user);
            }
            return View(user);
        }

        public IActionResult Cancel()
        {
            return View();
        }
    }
}
