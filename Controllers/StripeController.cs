﻿using System;
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
        public async Task<ActionResult> ProductAsync(string product, string price, string user)
        {
            var prod = await _stripeService.GetProductAsync(product);
            var priceObject = await _stripeService.GetPriceAsync(price);
            var parameters = new { productName = prod.Name };
            var sessionparameter = "&session={CHECKOUT_SESSION_ID}";
            var successPage = prod.Metadata.Keys.Contains("Success") ? prod.Metadata["Success"] : "Success";
            var successUrl = $"{Url.Action(successPage, "Payment", parameters, Request.Scheme)}{sessionparameter}";
            var cancelUrl = Url.Action("Cancel", "Payment", parameters, Request.Scheme);

            var stripeSessionId = _stripeService.StartPayment(
                prod.Id,
                priceObject.UnitAmount.Value,
                priceObject.Currency,
                user,
                successUrl,
                cancelUrl
                );

            var result = new JsonResult(new { id = stripeSessionId });
            return result;
        }

        [HttpPost("Generic")]
        public ActionResult Generic(string product, long price, string description, string user)
        {
            var parameters = new { productName = product };
            var sessionparameter = "&session={CHECKOUT_SESSION_ID}";
            var successUrl = $"{Url.Action("Success", "Payment", parameters, Request.Scheme)}{sessionparameter}";
            var cancelUrl = Url.Action("Cancel", "Payment", parameters, Request.Scheme);

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
