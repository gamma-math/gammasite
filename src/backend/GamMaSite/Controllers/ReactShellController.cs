using Microsoft.AspNetCore.Mvc;

namespace GamMaSite.Controllers
{
    /*
     * Dedicated endpoint for the SPA fallback. Keeping this action separate
     * from the explicitly mapped legacy routes avoids ambiguous MVC matches.
     */
    public class ReactShellController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/React/Index.cshtml");
        }
    }
}
