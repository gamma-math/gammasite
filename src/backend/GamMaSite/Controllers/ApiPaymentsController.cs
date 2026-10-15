using System;
using System.Threading.Tasks;
using GamMaSite.ViewModels.Api;
using GamMaSite.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Stripe;

namespace GamMaSite.Controllers
{
    [ApiController]
    [Route("api/payments")]
    [Authorize]
    /*
     * Provides product, configuration, and checkout session endpoints for React payments.
     */
    public class ApiPaymentsController : ControllerBase
    {
        private readonly IStripeService _stripeService;
        private readonly IConfiguration _configuration;

        public ApiPaymentsController(IStripeService stripeService, IConfiguration configuration)
        {
            _stripeService = stripeService;
            _configuration = configuration;
        }

        [HttpGet("config")]
        public IActionResult GetConfig()
        {
            return Ok(new PaymentConfigDto { PublicApiKey = _configuration["StripeConfig:PublicApiKey"] });
        }

        [HttpGet("products")]
        public async Task<IActionResult> GetProducts()
        {
            var productId = _configuration["StripeConfig:MembershipProductId"];
            var priceId = _configuration["StripeConfig:PaymentOneTimeId"];
            if (string.IsNullOrWhiteSpace(productId) || string.IsNullOrWhiteSpace(priceId))
            {
                return Problem("Stripe-medlemskabsproduktet er ikke konfigureret.");
            }

            var product = await _stripeService.GetProductAsync(productId);
            var price = await _stripeService.GetPriceByIdAsync(priceId);
            if (!IsPriceForProduct(price, product))
            {
                return NotFound();
            }

            return Ok(new[] { ToDto(product, price) });
        }

        [HttpGet("products/{id}")]
        public async Task<IActionResult> GetProduct(string id)
        {
            var configuredProductId = _configuration["StripeConfig:MembershipProductId"];
            var priceId = _configuration["StripeConfig:PaymentOneTimeId"];
            if (string.IsNullOrWhiteSpace(configuredProductId) ||
                !string.Equals(id, configuredProductId, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(priceId))
            {
                return NotFound();
            }

            var product = await _stripeService.GetProductAsync(configuredProductId);
            var price = await _stripeService.GetPriceByIdAsync(priceId);
            if (!IsPriceForProduct(price, product))
            {
                return NotFound();
            }

            return Ok(ToDto(product, price));
        }

        private static bool IsPriceForProduct(Price price, Product product)
        {
            return price != null && product != null &&
                string.Equals(price.ProductId, product.Id, StringComparison.Ordinal);
        }

        private static PaymentProductDto ToDto(Product product, Price price)
        {
            return new PaymentProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                UnitAmount = price?.UnitAmount,
                Currency = price?.Currency,
                Additional = Metadata(product, "Additional"),
                Conditions = Metadata(product, "Conditions"),
                ConditionsName = Metadata(product, "ConditionsName")
            };
        }

        private static string Metadata(Product product, string key)
        {
            return product.Metadata != null && product.Metadata.TryGetValue(key, out var value) ? value : string.Empty;
        }
    }
}
