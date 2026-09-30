using GamMaSite.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

/*
 * LEGACY MVC NOTE:
 * Calendar display is now handled by the React event pages and API endpoints.
 * Keep this controller temporarily as a fallback/reference until the old MVC
 * calendar view is removed.
 */
namespace GamMaSite.Controllers
{
    [Authorize]
    public class CalendarController : Controller
    {
        IICalService _icalService;

        public CalendarController(IICalService icalService)
        {
            this._icalService = icalService;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _icalService.GetEventsWrapper());
        }
    }
}
