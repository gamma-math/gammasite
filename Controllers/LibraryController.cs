using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GamMaSite.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GamMaSite.Controllers
{
    [Authorize]
    public class LibraryController : Controller
    {
        private readonly IIndexService _indexService;

        public LibraryController(IIndexService indexService)
        {
            this._indexService = indexService;
        }

        public async Task<IActionResult> IndexAsync(string path)
        {
            var pathName = !string.IsNullOrEmpty(path) ? path : "";
            var metas = new ContentMetas
            {
                Metas = await _indexService.GetContentMetasAsync(pathName)
            };
            if (metas.Metas.Count != 0) return View(metas);
            var content = await _indexService.GetContentAsync(pathName);
            return File(content.Content, content.MimeType);
        }

    }
}
