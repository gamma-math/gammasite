using GamMaSite.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
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

        // MVC action — kept until the React page is cut over
        public async Task<IActionResult> Index()
        {
            return View(await _icalService.GetEventsWrapper());
        }

        // REST endpoint for the React SPA
        [HttpGet("/api/calendar")]
        public async Task<IActionResult> GetEvents()
        {
            var wrapper = await _icalService.GetEventsWrapper();
            var events = wrapper.Events
                .OrderBy(e => e.Start.AsUtc)
                .Select(e => new
                {
                    uid = e.Uid,
                    summary = e.Summary,
                    startUtc = e.Start.AsUtc,
                    startLocal = e.ToStartLocalDateTime(),
                    weekday = e.ToStartWeekday(),
                    weekOfYear = e.ToWeekOfYear(),
                    location = e.Location,
                    locationMapsUrl = string.IsNullOrEmpty(e.Location)
                        ? null
                        : e.ToGoogleMapsAddress(),
                    description = e.Description,
                    isPast = e.Start.AsUtc < DateTime.UtcNow,
                })
                .ToList();

            return Ok(events);
        }
    }
}
