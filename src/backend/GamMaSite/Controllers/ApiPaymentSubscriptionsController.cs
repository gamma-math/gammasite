using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using GamMaSite.Data;
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

        public ApiPaymentSubscriptionsController(ApplicationDbContext db)
        {
            _db = db;
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
    }
}
