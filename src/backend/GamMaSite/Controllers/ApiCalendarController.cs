using System.Linq;
using System.Threading.Tasks;
using GamMaSite.Services;
using GamMaSite.ViewModels.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GamMaSite.Controllers
{
    [ApiController]
    [Route("api/calendar")]
    [Authorize]
    /*
     * Provides calendar data used by the React member calendar page.
     */
    public class ApiCalendarController : ControllerBase
    {
        private readonly IICalService _icalService;

        public ApiCalendarController(IICalService icalService)
        {
            _icalService = icalService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUpcoming()
        {
            var wrapper = await _icalService.GetEventsWrapper();
            return Ok(wrapper.UpcomingEvents.Select(item => new CalendarEventDto
            {
                Id = item.Uid,
                Title = item.Summary,
                StartsAt = item.ToStartLocalDateTime(),
                Weekday = item.ToStartWeekday(),
                WeekNumber = item.ToWeekOfYear(),
                Location = item.Location,
                MapsUrl = item.ToGoogleMapsAddress(),
                Description = item.Description
            }));
        }
    }
}
