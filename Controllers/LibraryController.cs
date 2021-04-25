using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GamMaSite.Models;
using GamMaSite.Services;
using GamMaSite.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace GamMaSite.Controllers
{
    [Authorize]
    public class LibraryController : Controller
    {
        private IIndexService _indexService;

        public LibraryController(IIndexService indexService)
        {
            this._indexService = indexService;
        }

        public async Task<IActionResult> IndexAsync(string path)
        {
            var pathName = !string.IsNullOrEmpty(path) ? path : "";
            var metaData = await _indexService.GetContentMetasAsync(pathName);
            if (metaData.Count == 0)
            {
                var content = await _indexService.GetContentAsync(pathName);
                return File(content.Content, content.MimeType);
            }
            return View(metaData);
        }

    }
}
