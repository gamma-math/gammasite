using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using GamMaSite.Models;
using Microsoft.AspNetCore.Authorization;

/*
 * LEGACY MVC NOTE:
 * Most Home MVC pages have been replaced by the React shell through ReactController.
 * Keep this controller for now because the Error action is still used by ASP.NET
 * exception handling, and the old actions are retained as fallback cleanup targets.
 */
namespace GamMaSite.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Arrangementer()
        {
            return View();
        }

        public IActionResult Betingelser()
        {
            return View();
        }

        public IActionResult Cookies()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
