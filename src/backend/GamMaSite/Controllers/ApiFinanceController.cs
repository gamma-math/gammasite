using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Claims;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GamMaSite.Services;
using GamMaSite.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GamMaSite.Controllers
{
    [ApiController]
    [Authorize]
    [AutoValidateAntiforgeryToken]
    [Route("api/finance")]
    public sealed class ApiFinanceController : ControllerBase
    {
        private readonly FinanceReportService _financeReportService;
        private readonly FinanceImportService _financeImportService;
        private readonly ApplicationDbContext _db;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IReceiptPdfService _receiptPdfService;

        public ApiFinanceController(
            FinanceReportService financeReportService,
            FinanceImportService financeImportService,
            ApplicationDbContext db,
            IEmailTemplateService emailTemplateService,
            IReceiptPdfService receiptPdfService)
        {
            _financeReportService = financeReportService;
            _financeImportService = financeImportService;
            _db = db;
            _emailTemplateService = emailTemplateService;
            _receiptPdfService = receiptPdfService;
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

        [HttpGet("postings/{id}/receipt")]
        public async Task<IActionResult> PreviewPostingReceipt(string id, CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var user = await _db.Users.AsNoTracking().SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
            if (user == null)
            {
                return Unauthorized();
            }

            var posting = await _financeReportService.GetUserPostingReceiptAsync(userId, id, cancellationToken);
            if (posting == null)
            {
                return NotFound();
            }

            var isPayment = posting.Amount < 0;
            var displayAmount = isPayment ? Math.Abs(posting.Amount) : -Math.Abs(posting.Amount);
            var templateType = isPayment
                ? SystemEmailTemplateService.FinancePaymentReceipt
                : SystemEmailTemplateService.FinanceRefundReceipt;
            var values = new Dictionary<string, string>
            {
                ["UserId"] = user.Id,
                ["UserName"] = user.Navn ?? "",
                ["UserEmail"] = user.Email ?? "",
                ["PostingId"] = posting.Id,
                ["Date"] = posting.Date,
                ["Product"] = posting.Product,
                ["PaymentMethod"] = posting.PaymentMethod,
                ["Amount"] = displayAmount.ToString("N2", CultureInfo.GetCultureInfo("da-DK")) + " kr.",
                ["TransactionType"] = isPayment ? "Betaling" : "Refusion",
                ["CompanyName"] = "Alumneforeningen GamMa",
                ["CompanyCvr"] = "34637768",
                ["PdfUrl"] = $"/api/finance/postings/{Uri.EscapeDataString(posting.Id)}/receipt/pdf"
            };
            var html = await _emailTemplateService.RenderActiveHtmlAsync(templateType, values);
            if (html == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Kvitteringsskabelonen mangler eller er inaktiv." });
            }

            return Content(html, "text/html; charset=utf-8");
        }

        [HttpGet("postings/{id}/receipt/pdf")]
        public async Task<IActionResult> DownloadPostingReceipt(string id, CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var user = await _db.Users.AsNoTracking().SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
            if (user == null)
            {
                return Unauthorized();
            }

            var posting = await _financeReportService.GetUserPostingReceiptAsync(userId, id, cancellationToken);
            if (posting == null)
            {
                return NotFound();
            }

            var isPayment = posting.Amount < 0;
            var displayAmount = isPayment ? Math.Abs(posting.Amount) : -Math.Abs(posting.Amount);
            var templateType = isPayment
                ? SystemEmailTemplateService.FinancePaymentReceipt
                : SystemEmailTemplateService.FinanceRefundReceipt;
            var values = new Dictionary<string, string>
            {
                ["UserId"] = user.Id,
                ["UserName"] = user.Navn ?? "",
                ["UserEmail"] = user.Email ?? "",
                ["PostingId"] = posting.Id,
                ["Date"] = posting.Date,
                ["Product"] = posting.Product,
                ["PaymentMethod"] = posting.PaymentMethod,
                ["Amount"] = displayAmount.ToString("N2", CultureInfo.GetCultureInfo("da-DK")) + " kr.",
                ["TransactionType"] = isPayment ? "Betaling" : "Refusion",
                ["CompanyName"] = "Alumneforeningen GamMa",
                ["CompanyCvr"] = "34637768",
                ["PdfUrl"] = $"/api/finance/postings/{Uri.EscapeDataString(posting.Id)}/receipt/pdf"
            };
            var html = await _emailTemplateService.RenderActiveHtmlAsync(templateType, values);
            if (html == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Kvitteringsskabelonen mangler eller er inaktiv." });
            }

            try
            {
                var pdf = await _receiptPdfService.RenderAsync(html, cancellationToken);
                return File(pdf, "application/pdf", $"GamMa-kvittering-{posting.Id}.pdf");
            }
            catch (InvalidOperationException exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = exception.Message });
            }
        }

        [HttpGet("admin/overview")]
        [Authorize(Policy = PermissionPolicies.FinanceViewAll)]
        public async Task<IActionResult> GetAdminOverview([FromQuery] int? year, CancellationToken cancellationToken)
        {
            var currentYear = DateTime.Today.Year;
            var selectedYear = year ?? currentYear;
            if (selectedYear < 1900 || selectedYear > 9999)
            {
                return BadRequest(new { error = "Det valgte år er ugyldigt." });
            }

            return Ok(await _financeReportService.GetAdminOverviewAsync(selectedYear, cancellationToken));
        }

        [HttpGet("admin/budgets")]
        [Authorize(Policy = PermissionPolicies.FinanceEditAll)]
        public async Task<IActionResult> GetAdminBudgets(CancellationToken cancellationToken)
        {
            return Ok(await _financeReportService.GetAdminBudgetsAsync(cancellationToken));
        }

        [HttpGet("admin/accounts")]
        [Authorize(Policy = PermissionPolicies.FinanceEditAll)]
        public async Task<IActionResult> GetAdminAccounts(CancellationToken cancellationToken)
        {
            return Ok(await _financeReportService.GetAdminAccountsAsync(cancellationToken));
        }

        [HttpPost("admin/budgets")]
        [Authorize(Policy = PermissionPolicies.FinanceEditAll)]
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
        [Authorize(Policy = PermissionPolicies.FinanceEditAll)]
        public async Task<IActionResult> GetAdminBudget(string id, CancellationToken cancellationToken)
        {
            var budget = await _financeReportService.GetAdminBudgetAsync(id, cancellationToken);
            return budget == null ? NotFound() : Ok(budget);
        }

        [HttpPut("admin/budgets/{id}")]
        [Authorize(Policy = PermissionPolicies.FinanceEditAll)]
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
        [Authorize(Policy = PermissionPolicies.FinanceEditAll)]
        public async Task<IActionResult> DeleteAdminBudget(string id, CancellationToken cancellationToken)
        {
            return await _financeReportService.DeleteAdminBudgetAsync(id, cancellationToken)
                ? NoContent()
                : NotFound();
        }

        [HttpGet("admin/posteringsgrupper")]
        [Authorize(Policy = PermissionPolicies.FinanceEditAll)]
        public async Task<IActionResult> GetAdminPostingGroups(CancellationToken cancellationToken)
        {
            return Ok(await _financeReportService.GetAdminPostingGroupsAsync(cancellationToken));
        }

        [HttpGet("admin/posteringsgrupper/{id}")]
        [Authorize(Policy = PermissionPolicies.FinanceEditAll)]
        public async Task<IActionResult> GetAdminPostingGroup(string id, CancellationToken cancellationToken)
        {
            var result = await _financeReportService.GetAdminPostingGroupAsync(id, cancellationToken);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost("admin/posteringsgrupper")]
        [Authorize(Policy = PermissionPolicies.FinanceEditAll)]
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
        [Authorize(Policy = PermissionPolicies.FinanceEditAll)]
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
        [Authorize(Policy = PermissionPolicies.FinanceEditAll)]
        public async Task<IActionResult> DeleteAdminPostingGroup(string id, CancellationToken cancellationToken)
        {
            return await _financeReportService.DeleteAdminPostingGroupAsync(id, cancellationToken)
                ? NoContent()
                : NotFound();
        }

        [HttpGet("admin/postings")]
        [Authorize(Policy = PermissionPolicies.FinanceViewAll)]
        public async Task<IActionResult> GetAdminPostings([FromQuery] int? year, [FromQuery] bool allYears, [FromQuery] string accountId, [FromQuery] long? bankKey, [FromQuery] long? mobilePayKey, [FromQuery] long? stripeKey, CancellationToken cancellationToken)
        {
            var currentYear = DateTime.Today.Year;
            int? selectedYear = allYears ? null : year ?? currentYear;
            if (selectedYear.HasValue && (selectedYear.Value < 1900 || selectedYear.Value > 9999))
            {
                return BadRequest(new { error = "Det valgte år er ugyldigt." });
            }

            return Ok(await _financeReportService.GetAdminPostingsAsync(selectedYear, accountId, bankKey, mobilePayKey, stripeKey, cancellationToken));
        }

        [HttpGet("admin/postings/years")]
        [Authorize(Policy = PermissionPolicies.FinanceViewAll)]
        public async Task<IActionResult> GetAdminPostingYears(CancellationToken cancellationToken)
        {
            return Ok(await _financeReportService.GetAdminPostingYearsAsync(cancellationToken));
        }

        [HttpGet("admin/postings/options")]
        [Authorize(Policy = PermissionPolicies.FinanceViewAll)]
        public async Task<IActionResult> GetPostingEditorOptions(CancellationToken cancellationToken)
        {
            return Ok(await _financeReportService.GetPostingEditorOptionsAsync(cancellationToken));
        }

        [HttpGet("admin/postings/{id}")]
        [Authorize(Policy = PermissionPolicies.FinanceViewAll)]
        public async Task<IActionResult> GetAdminPosting(string id, CancellationToken cancellationToken)
        {
            var posting = await _financeReportService.GetAdminPostingDetailAsync(id, cancellationToken);
            return posting == null ? NotFound() : Ok(posting);
        }

        [HttpPut("admin/postings/{id}")]
        [Authorize(Policy = PermissionPolicies.FinanceEditAll)]
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
        [Authorize(Policy = PermissionPolicies.FinanceEditAll)]
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
        [Authorize(Policy = PermissionPolicies.FinanceEditAll)]
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
        [Authorize(Policy = PermissionPolicies.FinanceEditAll)]
        public async Task<IActionResult> DeleteAdminPosting(string id, CancellationToken cancellationToken)
        {
            return await _financeReportService.DeleteAdminPostingAsync(id, cancellationToken)
                ? NoContent()
                : NotFound();
        }

        [HttpGet("admin/import/history")]
        [Authorize(Policy = PermissionPolicies.FinanceEditAll)]
        public async Task<IActionResult> GetImportHistory(CancellationToken cancellationToken)
        {
            return Ok(await _financeImportService.GetHistoryAsync(cancellationToken));
        }

        [HttpPost("admin/import/postings")]
        [Authorize(Policy = PermissionPolicies.FinanceEditAll)]
        public async Task<IActionResult> GenerateImportPostings(CancellationToken cancellationToken)
        {
            return Ok(await _financeImportService.GenerateDerivedPostingsAsync(cancellationToken));
        }

        [HttpGet("admin/import/templates/{source}")]
        [Authorize(Policy = PermissionPolicies.FinanceEditAll)]
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
                content = "Date;Timestamp;Amount;Message;Transaction Type;Transfer Reference;Transfer Date;Payment Transaction ID;User Name;Merchant Reference;Product\r\n25-10-2026;2026-10-25T23:04:59+01:00;10,00;Kontingent;Payment;02020976840012510261;26-10-2026;36855827025;;chr-eksempel;Billing and Recurring Payments\r\n";
            }
            else
            {
                return NotFound();
            }

            return File(Encoding.UTF8.GetBytes("\uFEFF" + content), "text/csv; charset=utf-8", fileName);
        }

        [HttpPost("admin/import")]
        [Authorize(Policy = PermissionPolicies.FinanceEditAll)]
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
                    SafeFileName(bankFile),
                    mobilePayStream,
                    SafeFileName(mobilePayFile),
                    syncPostings,
                    cancellationToken));
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { error = exception.Message });
            }
        }

        [HttpPost("admin/import/validate")]
        [Authorize(Policy = PermissionPolicies.FinanceEditAll)]
        [RequestSizeLimit(20 * 1024 * 1024)]
        public IActionResult ValidateImportCsv(
            [FromForm] IFormFile bankFile,
            [FromForm] IFormFile mobilePayFile)
        {
            try
            {
                ValidateCsvFile(bankFile, "Bank CSV");
                ValidateCsvFile(mobilePayFile, "MobilePay CSV");
                if (bankFile == null && mobilePayFile == null)
                {
                    return BadRequest(new { error = "Vælg mindst én CSV-fil." });
                }

                using var bankStream = bankFile?.OpenReadStream();
                using var mobilePayStream = mobilePayFile?.OpenReadStream();
                _financeImportService.ValidateCsv(bankStream, mobilePayStream);
                return Ok(new { valid = true });
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
            if (!string.Equals(Path.GetExtension(SafeFileName(file)), ".csv", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"{label} skal være en CSV-fil.");
            }
        }

        private static string SafeFileName(IFormFile file)
        {
            var fileName = file?.FileName ?? string.Empty;
            return Path.GetFileName(fileName.Replace('\\', '/'));
        }
    }
}
