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

        [HttpGet("admin/overview")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminOverview([FromQuery] int? year, CancellationToken cancellationToken)
        {
            var currentYear = DateTime.Today.Year;
            var selectedYear = year ?? currentYear;
            if (selectedYear != currentYear && selectedYear != currentYear - 1)
            {
                return BadRequest(new { error = "Der kan kun vÃ¦lges dette Ã¥r eller sidste Ã¥r." });
            }

            return Ok(await _financeReportService.GetAdminOverviewAsync(selectedYear, cancellationToken));
        }

        [HttpGet("admin/postings")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminPostings([FromQuery] int? year, [FromQuery] string accountId, [FromQuery] long? bankKey, [FromQuery] long? mobilePayKey, CancellationToken cancellationToken)
        {
            var currentYear = DateTime.Today.Year;
            var selectedYear = year ?? currentYear;
            if (selectedYear != currentYear && selectedYear != currentYear - 1)
            {
                return BadRequest(new { error = "Der kan kun vÃ¦lges dette Ã¥r eller sidste Ã¥r." });
            }

            return Ok(await _financeReportService.GetAdminPostingsAsync(selectedYear, accountId, bankKey, mobilePayKey, cancellationToken));
        }

        [HttpGet("admin/postings/options")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPostingEditorOptions(CancellationToken cancellationToken)
        {
            return Ok(await _financeReportService.GetPostingEditorOptionsAsync(cancellationToken));
        }

        [HttpGet("admin/postings/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminPosting(string id, CancellationToken cancellationToken)
        {
            var posting = await _financeReportService.GetAdminPostingDetailAsync(id, cancellationToken);
            return posting == null ? NotFound() : Ok(posting);
        }

        [HttpPut("admin/postings/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateAdminPosting(string id, [FromBody] FinanceAdminPostingUpdateDto update, CancellationToken cancellationToken)
        {
            return await _financeReportService.UpdateAdminPostingAsync(id, update, cancellationToken) ? NoContent() : NotFound();
        }
    }
}
