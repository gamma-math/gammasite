using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using GamMaSite.Data;
using GamMaSite.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GamMaSite.Controllers
{
    [ApiController]
    [Authorize]
    [AutoValidateAntiforgeryToken]
    [Route("api/account/subscriptions")]
    public sealed class ApiPaymentSubscriptionsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IStripeService _stripeService;

        public ApiPaymentSubscriptionsController(ApplicationDbContext db, IStripeService stripeService = null)
        {
            _db = db;
            _stripeService = stripeService;
        }

        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var subscriptions = await _db.PaymentSubscriptions
                .AsNoTracking()
                .Where(subscription => subscription.UserId == userId)
                .OrderByDescending(subscription => subscription.Created)
                .Select(subscription => new
                {
                    providerSubscriptionId = subscription.ProviderSubscriptionId,
                    provider = subscription.Provider,
                    productName = subscription.ProductName,
                    status = subscription.Status,
                    lastPaidAt = subscription.LastPaidAt,
                    nextChargeAt = subscription.NextChargeAt,
                    created = subscription.Created,
                    updated = subscription.Updated
                })
                .ToListAsync(cancellationToken);

            return Ok(subscriptions);
        }

        [HttpDelete("{providerSubscriptionId}")]
        public async Task<IActionResult> Delete(string providerSubscriptionId, CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            if (_stripeService == null)
            {
                return Problem("Betalingstjenesten er ikke konfigureret.");
            }

            var subscription = await _db.PaymentSubscriptions
                .SingleOrDefaultAsync(item => item.UserId == userId &&
                    item.Provider == "Stripe" &&
                    item.ProviderSubscriptionId == providerSubscriptionId, cancellationToken);
            if (subscription == null)
            {
                return NotFound();
            }

            var canceledSubscription = await _stripeService.CancelSubscriptionAsync(providerSubscriptionId);
            if (canceledSubscription == null)
            {
                return BadRequest("Abonnementet kunne ikke annulleres hos betalingsudbyderen.");
            }

            subscription.Status = canceledSubscription.Status;
            subscription.CancelAtPeriodEnd = canceledSubscription.CancelAtPeriodEnd;
            subscription.Updated = System.DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                status = subscription.Status,
                canceled = true
            });
        }
    }
}
