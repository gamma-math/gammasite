using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GamMaSite.Services;
using GamMaSite.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GamMaSite.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        IStripeService _stripeService;

        public PaymentController(IStripeService stripeService)
        {
            this._stripeService = stripeService;
        }

        public async Task<IActionResult> IndexAsync()
        {
            return View(await _stripeService.GetAllProductsAsync());
        }

        public async Task<IActionResult> ProductAsync(string id)
        {
            var product = await _stripeService.GetProductAsync(id);
            var prices = await _stripeService.GetPricesAsync(id);
            return View(new ProductInfo(product, prices));
        }

        public async Task<IActionResult> KontingentAsync()
        {
            var kontingent = (await _stripeService.GetAllProductsAsync())
                .Where(p => p.Name.ToLower().Contains("kontingent")).FirstOrDefault();
            return RedirectToAction("Product", new { id = kontingent.Id }); ;
        }

        public IActionResult Generisk()
        {
            return View();
        }

        public IActionResult Success()
        {
            return View();
        }

        public IActionResult Cancel()
        {
            return View();
        }
    }
}
