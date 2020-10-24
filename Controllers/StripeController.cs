using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using GamMaSite.Services;

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

        [HttpPost]
        public ActionResult Create()
        {
            var domain = Url.PageLink();
            var successUrl = domain + "/success.html";
            var cancelUrl = domain + "/cancel.html";

            var stripeSessionId = _stripeService.StartPayment(2000, "dkk", "Generisk Betaling", "payment", successUrl, cancelUrl);

            var result = new JsonResult(new { id = stripeSessionId });
            return result;
        }
    }
}
