using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using GamMaSite.Models;
using GamMaSite.Services;
using GamMaSite.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

/*
 * LEGACY MVC NOTE:
 * Most payment UI pages have moved behind React routes and Stripe API endpoints.
 * Review this controller carefully before removal because some success/cancel
 * payment routes may still be used by Stripe redirects or legacy links.
 */
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
            var configuredProductId = _configuration["StripeConfig:MembershipProductId"];
            var priceId = _configuration["StripeConfig:PaymentOneTimeId"];
            if (string.IsNullOrWhiteSpace(configuredProductId) ||
                !string.Equals(id, configuredProductId, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(priceId))
            {
                return RedirectToAction("Index");
            }

            var product = await _stripeService.GetProductAsync(configuredProductId);
            var price = await _stripeService.GetPriceByIdAsync(priceId);
            if (product != null && price != null &&
                string.Equals(price.ProductId, product.Id, StringComparison.Ordinal))
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
            var configuredProductId = _configuration["StripeConfig:MembershipProductId"];
            var kontingentProduct = await _stripeService.GetProductAsync(configuredProductId);
            var stripeSession = await _stripeService.GetSessionAsync(session);
            var hasPayed = _stripeService.IsPaymentComplete(stripeSession);
            var sessionProductId = stripeSession?.Metadata != null &&
                stripeSession.Metadata.TryGetValue("Product", out var productId)
                ? productId
                : null;
            var correctProduct = kontingentProduct != null &&
                sessionProductId == kontingentProduct.Id;
            var user = await _userManager.FindByIdAsync(stripeSession?.ClientReferenceId);
            var sessionCreated = TryGetSessionCreated(stripeSession);
            if (sessionCreated.HasValue && DateTime.UtcNow < sessionCreated.Value.AddHours(1) && hasPayed && correctProduct && user != null)
            {
                if (!string.Equals(stripeSession.Mode, "subscription", StringComparison.OrdinalIgnoreCase))
                {
                    user.MarkAsPayed();
                    await _userManager.UpdateAsync(user);
                }

                return View(user);
            }
            else
            {
                return RedirectToAction("Index");
            }
        }

        private static DateTime? TryGetSessionCreated(Stripe.Checkout.Session session)
        {
            if (session?.Metadata == null ||
                !session.Metadata.TryGetValue("SessionCreated", out var value) ||
                string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed)
                ? parsed
                : null;
        }

        public IActionResult Cancel()
        {
            return View();
        }
    }
}
