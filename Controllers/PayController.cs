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

        // ── REST API endpoints for the React SPA ───────────────────────────────

        // Verifies a completed kontingent Stripe session and marks the user as paid.
        // Called by KontingentSuccessPage on mount — Stripe redirects the browser to
        // /pay/kontingent-success?session={CHECKOUT_SESSION_ID} after payment.
        [HttpGet("/api/pay/kontingent-success")]
        public async Task<IActionResult> KontingentSuccessAsync([FromQuery] string session)
        {
            if (string.IsNullOrEmpty(session))
                return BadRequest(new { error = "Manglende session" });

            var kontingentProduct = await _stripeService.GetProductByNameAsync("kontingent");
            var stripeSession = await _stripeService.GetSessionAsync(session);
            var hasPayed = _stripeService.IsPaymentComplete(stripeSession);
            var correctProduct = stripeSession?.Metadata?["Product"] == kontingentProduct.Id;
            var user = await _userManager.FindByIdAsync(stripeSession?.ClientReferenceId);
            var sessionCreated = stripeSession != null ? DateTime.Parse(stripeSession.Metadata["SessionCreated"]) : DateTime.MinValue;

            if (DateTime.UtcNow < sessionCreated.AddHours(1) && hasPayed && correctProduct && user is not null)
            {
                user.MarkAsPayed();
                await _userManager.UpdateAsync(user);
                return Ok(new { success = true });
            }

            return Ok(new { success = false });
        }

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
