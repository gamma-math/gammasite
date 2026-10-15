using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GamMaSite.Data;
using GamMaSite.Models;
using Microsoft.AspNetCore.Mvc;
using GamMaSite.Services;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

namespace GamMaSite.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    [AutoValidateAntiforgeryToken]
    public class StripeController : ControllerBase
    {
        private readonly IStripeService _stripeService;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _db;

        public StripeController(IStripeService stripeService, IConfiguration configuration, ApplicationDbContext db)
        {
            this._stripeService = stripeService;
            this._configuration = configuration;
            this._db = db;
        }

        [HttpPost("Product")]
        public async Task<ActionResult> ProductAsync(string product, string user, string paymentType = "one-time")
        {
            var configuredProductId = _configuration["StripeConfig:MembershipProductId"];
            var isRecurring = string.Equals(paymentType, "recurring", StringComparison.OrdinalIgnoreCase);
            if (!isRecurring && !string.Equals(paymentType, "one-time", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Ugyldig betalingstype.");
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(currentUserId))
            {
                user = currentUserId;
            }

            if (!string.IsNullOrWhiteSpace(user))
            {
                var member = await _db.Users
                    .AsNoTracking()
                    .SingleOrDefaultAsync(member => member.Id == user, HttpContext.RequestAborted);
                var hasPaidMembership = member?.Status == UserStatus.BETALT || member?.Status == UserStatus.STUDERENDE;
                var hasActiveSubscription = await _db.PaymentSubscriptions.AnyAsync(subscription =>
                    subscription.UserId == user && (subscription.Status == PaymentSubscriptionStatuses.Active ||
                        subscription.Status == PaymentSubscriptionStatuses.Trialing ||
                        subscription.Status == PaymentSubscriptionStatuses.PastDue ||
                        subscription.Status == PaymentSubscriptionStatuses.Incomplete),
                    HttpContext.RequestAborted);

                if (hasPaidMembership || hasActiveSubscription)
                {
                    return Conflict("Du har allerede betalt medlemskab.");
                }
            }

            var priceId = _configuration[isRecurring
                ? "StripeConfig:PaymentRecurringId"
                : "StripeConfig:PaymentOneTimeId"];
            if (string.IsNullOrWhiteSpace(configuredProductId) ||
                !string.Equals(product, configuredProductId, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(priceId))
            {
                return NotFound();
            }

            var prod = await _stripeService.GetProductAsync(configuredProductId);
            var price = await _stripeService.GetPriceByIdAsync(priceId);
            if (prod == null || price == null ||
                !string.Equals(price.ProductId, prod.Id, StringComparison.Ordinal) ||
                !string.Equals(price.Type, isRecurring ? "recurring" : "one_time", StringComparison.Ordinal))
            {
                return NotFound();
            }

            var sessionparameter = "?session={CHECKOUT_SESSION_ID}";
            var successPage = prod.Metadata.Keys.Contains("Success") ? prod.Metadata["Success"] : "Success";
            var successUrl = $"{Url.Action(successPage, "Pay", new { }, Request.Scheme)}{sessionparameter}";
            var cancelUrl = Url.Action("Cancel", "Pay", new { }, Request.Scheme);

            var stripeSessionId = _stripeService.StartPayment(
                prod,
                price,
                user,
                successUrl,
                cancelUrl,
                isRecurring ? "subscription" : "payment"
                );

            var result = new JsonResult(new { id = stripeSessionId });
            return result;
        }

        [HttpPost("Generic")]
        public ActionResult Generic(string product, long price, string description, string user)
        {
            var sessionparameter = "?session={CHECKOUT_SESSION_ID}";
            var successUrl = $"{Url.Action("Success", "Pay", new { }, Request.Scheme)}{sessionparameter}";
            var cancelUrl = Url.Action("Cancel", "Pay", new { }, Request.Scheme);

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
