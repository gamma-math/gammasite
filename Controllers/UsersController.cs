using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GamMaSite.Areas.Identity.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GamMaSite.Controllers
{
    
    public class UsersController : Controller
    {
        private UserManager<GamMaUser> userManager;

        public UsersController(UserManager<GamMaUser> usrMgr)
        {
            userManager = usrMgr;
        }

        [Authorize]
        public IActionResult Index()
        {
            return View(userManager.Users);
        }
    }
}
