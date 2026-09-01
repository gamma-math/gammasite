using System.Linq;
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
            var products = await _stripeService.GetAllProductsAsync();
            var results = await Task.WhenAll(products.Select(async product =>
            {
                var price = await _stripeService.GetPriceAsync(product.Id);
                return ToDto(product, price);
            }));

            return Ok(results);
        }

        [HttpGet("products/{id}")]
        public async Task<IActionResult> GetProduct(string id)
        {
            var product = await _stripeService.GetProductAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            var price = await _stripeService.GetPriceAsync(id);
            return Ok(ToDto(product, price));
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
