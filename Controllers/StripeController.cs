using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using GamMaSite.Services;
using Microsoft.AspNetCore.Mvc.Routing;

namespace GamMaSite.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StripeController : ControllerBase
    {
        IStripeService _stripeService;

        public StripeController(IStripeService stripeService)
        {
            this._stripeService = stripeService;
        }

        [HttpPost("Product")]
        public async Task<ActionResult> ProductAsync(string product, string price)
        {
            var prod = await _stripeService.GetProductAsync(product);
            var priceObject = await _stripeService.GetPriceAsync(price);
            var successUrl = Url.Action("Success", "Payment", new { }, Request.Scheme);
            var cancelUrl = Url.Action("Cancel", "Payment", new { }, Request.Scheme);

            var stripeSessionId = _stripeService.StartPayment(
                prod.Id,
                priceObject.UnitAmount.Value,
                priceObject.Currency,
                successUrl,
                cancelUrl
                );

            var result = new JsonResult(new { id = stripeSessionId });
            return result;
        }

        [HttpPost("Generic")]
        public ActionResult Generic(string name, long price, string description)
        {
            var successUrl = Url.Action("Success", "Payment", new { }, Request.Scheme);
            var cancelUrl = Url.Action("Cancel", "Payment", new { }, Request.Scheme);

            var stripeSessionId = _stripeService.StartPayment(
                name, 
                description, 
                price, 
                "dkk", 
                successUrl, 
                cancelUrl
                );

            var result = new JsonResult(new { id = stripeSessionId });
            return result;
        }
    }
}
