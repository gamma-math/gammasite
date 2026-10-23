using Microsoft.AspNetCore.Mvc;

namespace GamMaSite.Controllers
{
    /*
     * Serves the React shell for canonical public routes and selected legacy MVC URLs.
     */
    public class ReactController : Controller
    {
        [HttpGet("/")]
        [HttpGet("/Home")]
        [HttpGet("/Home/Index")]
        [HttpGet("/Home/Arrangementer")]
        [HttpGet("/Home/Betingelser")]
        [HttpGet("/Home/Cookies")]
        [HttpGet("/calendar")]
        [HttpGet("/Library")]
        [HttpGet("/Users")]
        [HttpGet("/Users/Expanded")]
        [HttpGet("/Users/UpdateMass")]
        [HttpGet("/Role")]
        [HttpGet("/Role/Create")]
        [HttpGet("/Role/Update/{id?}")]
        [HttpGet("/Messages")]
        [HttpGet("/Pay")]
        [HttpGet("/Pay/Index")]
        [HttpGet("/Pay/Success")]
        [HttpGet("/Pay/Cancel")]
        public IActionResult Index()
        {
            return View();
        }
    }
}
