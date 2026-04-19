using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GamMaSite.Models;
using GamMaSite.Services;
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

        // KontingentSuccess: Stripe redirects here after a kontingent payment.
        // Server-side verification required — cannot be a React page.
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
                return Redirect("/app/pay");
            }
        }

        // ── REST API endpoints for the React SPA ───────────────────────────────

        // List all active Stripe products with their price. Also returns the
        // Stripe public key so the SPA can initialise stripe-js without a
        // separate config endpoint.
        [HttpGet("/api/pay/products")]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _stripeService.GetAllProductsAsync();
            var publicKey = _configuration["StripeConfig:PublicApiKey"];

            var result = new List<object>();
            foreach (var p in products)
            {
                var price = await _stripeService.GetPriceAsync(p.Id);
                result.Add(new
                {
                    id = p.Id,
                    name = p.Name,
                    description = p.Description,
                    priceOre = price?.UnitAmount,
                    currency = price?.Currency,
                });
            }

            return Ok(new { publicKey, products = result });
        }

        // Single product detail — used by the product detail / checkout page.
        [HttpGet("/api/pay/products/{id}")]
        public async Task<IActionResult> GetProduct(string id)
        {
            var product = await _stripeService.GetProductAsync(id);
            if (product is null) return NotFound();

            var price = await _stripeService.GetPriceAsync(id);
            var publicKey = _configuration["StripeConfig:PublicApiKey"];

            return Ok(new
            {
                id = product.Id,
                name = product.Name,
                description = product.Description,
                priceOre = price?.UnitAmount,
                currency = price?.Currency,
                metadata = product.Metadata,
                publicKey,
            });
        }
    }
}
