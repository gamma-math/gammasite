using GamMaSite.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

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
