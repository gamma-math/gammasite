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
using Microsoft.Extensions.Configuration;

namespace GamMaSite.Controllers
{
    [Authorize]
    public class PayController : Controller
    {
        private readonly UserManager<SiteUser> _userManager;

        private readonly IStripeService _stripeService;

        private readonly IConfiguration _configuration;

        public PayController(UserManager<SiteUser> usrMgr, IStripeService stripe, IConfiguration conf)
        {
            this._userManager = usrMgr;
            this._stripeService = stripe;
            this._configuration = conf;
        }

        public async Task<IActionResult> IndexAsync()
        {
            return View(await _stripeService.GetAllProductsAsync());
        }

        public async Task<IActionResult> ProductAsync(string id)
        {
            var product = await _stripeService.GetProductAsync(id);
            var price = await _stripeService.GetPriceAsync(id);
            if (product != null && price != null)
            {
                var apiKey = _configuration["StripeConfig:PublicApiKey"];
                return View(new ProductInfo(product, price, apiKey));
            }
            else
            {
                return RedirectToAction("Index");
            }
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
            var kontingentProduct = await _stripeService.GetProductByNameAsync("kontingent");
            var stripeSession = await _stripeService.GetSessionAsync(session);
            var hasPayed = _stripeService.IsPaymentComplete(stripeSession);
            var correctProduct = stripeSession?.Metadata?["Product"] == kontingentProduct.Id;
            var user = await _userManager.FindByIdAsync(stripeSession?.ClientReferenceId);
            var sessionCreated = stripeSession != null ? DateTime.Parse(stripeSession.Metadata["SessionCreated"]) : DateTime.MinValue;
            if (DateTime.UtcNow < sessionCreated.AddHours(1) && hasPayed && correctProduct)
            {
                user?.MarkAsPayed();
                await _userManager.UpdateAsync(user);
                return View(user);
            }
            else
            {
                return RedirectToAction("Index");
            }
        }

        public IActionResult Cancel()
        {
            return View();
        }
    }
}
