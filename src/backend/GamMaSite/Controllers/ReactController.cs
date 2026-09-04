using Microsoft.AspNetCore.Mvc;

namespace GamMaSite.Controllers
{
    /*
     * Serves the React shell for both new /react routes and selected legacy MVC URLs.
     */
    public class ReactController : Controller
    {
        [HttpGet("/")]
        [HttpGet("/Home")]
        [HttpGet("/Home/Index")]
        [HttpGet("/Home/Arrangementer")]
        [HttpGet("/Home/Betingelser")]
        [HttpGet("/Home/Cookies")]
        [HttpGet("/Calendar")]
        [HttpGet("/Users")]
        [HttpGet("/Users/Expanded")]
        [HttpGet("/Users/UpdateMass")]
        [HttpGet("/Role")]
        [HttpGet("/Role/Create")]
        [HttpGet("/Role/Update/{id?}")]
        [HttpGet("/Messages")]
        [HttpGet("/Pay")]
        [HttpGet("/Pay/Index")]
        [HttpGet("/Pay/Product/{id?}")]
        [HttpGet("/Pay/Generisk")]
        [HttpGet("/Pay/Success")]
        [HttpGet("/Pay/Cancel")]
        [HttpGet("/react")]
        [HttpGet("/react/{**path}")]
        public IActionResult Index()
        {
            return View();
        }
    }
}
