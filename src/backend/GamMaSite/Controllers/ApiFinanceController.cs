using System;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GamMaSite.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GamMaSite.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/finance")]
    public sealed class ApiFinanceController : ControllerBase
    {
        private readonly FinanceReportService _financeReportService;
        private readonly FinanceImportService _financeImportService;

        public ApiFinanceController(FinanceReportService financeReportService, FinanceImportService financeImportService)
        {
            _financeReportService = financeReportService;
            _financeImportService = financeImportService;
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

        [HttpGet("admin/import/history")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetImportHistory(CancellationToken cancellationToken)
        {
            return Ok(await _financeImportService.GetHistoryAsync(cancellationToken));
        }

        [HttpPost("admin/import/postings")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GenerateImportPostings(CancellationToken cancellationToken)
        {
            return Ok(await _financeImportService.GenerateDerivedPostingsAsync(cancellationToken));
        }

        [HttpGet("admin/import/templates/{source}")]
        [Authorize(Roles = "Admin")]
        public IActionResult DownloadImportTemplate(string source)
        {
            string content;
            string fileName;
            if (string.Equals(source, "bank", StringComparison.OrdinalIgnoreCase))
            {
                fileName = "eksempel-bankkontoudtog.csv";
                content = "Dato;Tekst;Beløb;Saldo\r\n03.09.2026;02000227660010309261;-244,00;12500,50\r\n";
            }
            else if (string.Equals(source, "mobilepay", StringComparison.OrdinalIgnoreCase))
            {
                fileName = "eksempel-mobilepay-transaktioner.csv";
                content = "Date;Timestamp;Amount;Message;Transaction Type;Transfer Reference;Transfer Date;Payment Transaction ID;User Name\r\n02-09-2026;2026-09-02T18:28:10+02:00;50,00;Eksempelbetaling;Payment;02000227660010309261;03-09-2026;36332578042;Eksempel Bruger\r\n";
            }
            else
            {
                return NotFound();
            }

            return File(Encoding.UTF8.GetBytes("\uFEFF" + content), "text/csv; charset=utf-8", fileName);
        }

        [HttpPost("admin/import")]
        [Authorize(Roles = "Admin")]
        [RequestSizeLimit(20 * 1024 * 1024)]
        public async Task<IActionResult> ImportCsv(
            [FromForm] IFormFile bankFile,
            [FromForm] IFormFile mobilePayFile,
            [FromForm] bool syncPostings = true,
            CancellationToken cancellationToken = default)
        {
            try
            {
                ValidateCsvFile(bankFile, "Bank CSV");
                ValidateCsvFile(mobilePayFile, "MobilePay CSV");
                if (bankFile == null && mobilePayFile == null)
                {
                    return BadRequest(new { error = "Vælg mindst én CSV-fil." });
                }

                await using var bankStream = bankFile?.OpenReadStream();
                await using var mobilePayStream = mobilePayFile?.OpenReadStream();
                return Ok(await _financeImportService.ImportAsync(
                    bankStream,
                    bankFile?.FileName,
                    mobilePayStream,
                    mobilePayFile?.FileName,
                    syncPostings,
                    cancellationToken));
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { error = exception.Message });
            }
        }

        private static void ValidateCsvFile(IFormFile file, string label)
        {
            if (file == null) return;
            if (file.Length == 0) throw new ArgumentException($"{label} er tom.");
            if (file.Length > 10 * 1024 * 1024) throw new ArgumentException($"{label} må højst være 10 MB.");
            if (!string.Equals(Path.GetExtension(file.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"{label} skal være en CSV-fil.");
            }
        }
    }
}
