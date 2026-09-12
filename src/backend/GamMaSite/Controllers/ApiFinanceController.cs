using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using GamMaSite.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GamMaSite.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/finance")]
    public sealed class ApiFinanceController : ControllerBase
    {
        private readonly FinanceReportService _financeReportService;

        public ApiFinanceController(FinanceReportService financeReportService)
        {
            _financeReportService = financeReportService;
        }

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview([FromQuery] int? year, CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var currentYear = DateTime.Today.Year;
            var selectedYear = year ?? currentYear;
            if (selectedYear != currentYear && selectedYear != currentYear - 1)
            {
                return BadRequest(new { error = "Der kan kun vælges dette år eller sidste år." });
            }

            return Ok(await _financeReportService.GetOverviewAsync(userId, selectedYear, cancellationToken));
        }

        [HttpGet("postings")]
        public async Task<IActionResult> GetPostings(CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            return Ok(await _financeReportService.GetUserPostingsAsync(userId, cancellationToken));
        }
    }
}
