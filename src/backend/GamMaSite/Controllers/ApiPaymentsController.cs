using GamMaSite.ViewModels.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace GamMaSite.Controllers
{
    [ApiController]
    [Route("api/payments")]
    [Authorize]
    /* Provides shared payment configuration for the React PaymentOffers flow. */
    public class ApiPaymentsController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ApiPaymentsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("config")]
        public IActionResult GetConfig()
        {
            return Ok(new PaymentConfigDto { PublicApiKey = _configuration["StripeConfig:PublicApiKey"] });
        }

    }
}
