using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace GamMaSite.Controllers
{
    public class PaymentController : Controller
    {
        public IActionResult Kontingent()
        {
            return View();
        }
    }
}
