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

        [HttpGet("admin/budgets")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminBudgets(CancellationToken cancellationToken)
        {
            return Ok(await _financeReportService.GetAdminBudgetsAsync(cancellationToken));
        }

        [HttpGet("admin/accounts")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminAccounts(CancellationToken cancellationToken)
        {
            return Ok(await _financeReportService.GetAdminAccountsAsync(cancellationToken));
        }

        [HttpPost("admin/budgets")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateAdminBudget([FromBody] FinanceBudgetUpdateDto update, CancellationToken cancellationToken)
        {
            try
            {
                return Ok(await _financeReportService.CreateAdminBudgetAsync(update, cancellationToken));
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { error = exception.Message });
            }
        }

        [HttpGet("admin/budgets/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminBudget(string id, CancellationToken cancellationToken)
        {
            var budget = await _financeReportService.GetAdminBudgetAsync(id, cancellationToken);
            return budget == null ? NotFound() : Ok(budget);
        }

        [HttpPut("admin/budgets/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateAdminBudget(string id, [FromBody] FinanceBudgetUpdateDto update, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _financeReportService.UpdateAdminBudgetAsync(id, update, cancellationToken);
                return result == null ? NotFound() : Ok(result);
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { error = exception.Message });
            }
        }

        [HttpDelete("admin/budgets/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAdminBudget(string id, CancellationToken cancellationToken)
        {
            return await _financeReportService.DeleteAdminBudgetAsync(id, cancellationToken)
                ? NoContent()
                : NotFound();
        }

        [HttpGet("admin/posteringsgrupper")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminPostingGroups(CancellationToken cancellationToken)
        {
            return Ok(await _financeReportService.GetAdminPostingGroupsAsync(cancellationToken));
        }

        [HttpGet("admin/posteringsgrupper/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminPostingGroup(string id, CancellationToken cancellationToken)
        {
            var result = await _financeReportService.GetAdminPostingGroupAsync(id, cancellationToken);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost("admin/posteringsgrupper")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateAdminPostingGroup([FromBody] FinancePostingGroupUpdateDto update, CancellationToken cancellationToken)
        {
            try
            {
                return Ok(await _financeReportService.CreateAdminPostingGroupAsync(update, cancellationToken));
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { error = exception.Message });
            }
        }

        [HttpPut("admin/posteringsgrupper/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateAdminPostingGroup(string id, [FromBody] FinancePostingGroupUpdateDto update, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _financeReportService.UpdateAdminPostingGroupAsync(id, update, cancellationToken);
                return result == null ? NotFound() : Ok(result);
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { error = exception.Message });
            }
        }

        [HttpDelete("admin/posteringsgrupper/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAdminPostingGroup(string id, CancellationToken cancellationToken)
        {
            return await _financeReportService.DeleteAdminPostingGroupAsync(id, cancellationToken)
                ? NoContent()
                : NotFound();
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
            try
            {
                return await _financeReportService.UpdateAdminPostingAsync(id, update, cancellationToken) ? NoContent() : NotFound();
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { error = exception.Message });
            }
        }

        [HttpPost("admin/postings")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateAdminPosting([FromBody] FinanceAdminPostingUpdateDto update, CancellationToken cancellationToken)
        {
            try
            {
                return Ok(await _financeReportService.CreateAdminPostingAsync(update, cancellationToken));
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { error = exception.Message });
            }
        }

        [HttpPost("admin/postings/{id}/duplicate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DuplicateAdminPosting(string id, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _financeReportService.DuplicateAdminPostingAsync(id, cancellationToken);
                return result == null ? NotFound() : Ok(result);
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { error = exception.Message });
            }
        }

        [HttpDelete("admin/postings/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAdminPosting(string id, CancellationToken cancellationToken)
        {
            return await _financeReportService.DeleteAdminPostingAsync(id, cancellationToken)
                ? NoContent()
                : NotFound();
        }
    }
}
