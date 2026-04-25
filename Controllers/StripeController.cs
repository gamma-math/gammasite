using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using GamMaSite.Services;
using Microsoft.AspNetCore.Authorization;

namespace GamMaSite.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class StripeController : ControllerBase
    {
        private IStripeService _stripeService;

        public StripeController(IStripeService stripeService)
        {
            this._stripeService = stripeService;
        }

        [HttpPost("Product")]
        public async Task<ActionResult> ProductAsync(string product, string user)
        {
            var prod = await _stripeService.GetProductAsync(product);
            var price = await _stripeService.GetPriceAsync(product);
            var sessionparameter = "?session={CHECKOUT_SESSION_ID}";
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            // Products with a "Success" metadata key use the kontingent-specific SPA success page,
            // which calls the server-side verification endpoint on mount.
            // All other products redirect to the generic React SPA success page.
            var successUrl = prod.Metadata.Keys.Contains("Success")
                ? $"{baseUrl}/app/pay/kontingent-success{sessionparameter}"
                : $"{baseUrl}/app/pay/success{sessionparameter}";
            var cancelUrl = $"{baseUrl}/app/pay/cancel";

            var stripeSessionId = _stripeService.StartPayment(
                prod,
                price,
                user,
                successUrl,
                cancelUrl
                );

            var session = await _stripeService.GetSessionAsync(stripeSessionId);
            var result = new JsonResult(new { id = stripeSessionId, url = session?.Url });
            return result;
        }

        [HttpPost("Generic")]
        public ActionResult Generic(string product, long price, string description, string user)
        {
            var sessionparameter = "?session={CHECKOUT_SESSION_ID}";
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var successUrl = $"{baseUrl}/app/pay/success{sessionparameter}";
            var cancelUrl = $"{baseUrl}/app/pay/cancel";

            var stripeSessionId = _stripeService.StartPayment(
                product,
                description,
                price,
                "dkk",
                user,
                successUrl,
                cancelUrl
                );

            var result = new JsonResult(new { id = stripeSessionId });
            return result;
        }
    }
}
