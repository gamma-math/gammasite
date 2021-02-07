using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GamMaSite.Models;
using GamMaSite.Services;
using GamMaSite.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        public async Task<IActionResult> IndexAsync(string query, string type)
        {
            var isEmpty = !string.IsNullOrEmpty(query);
            if (type == "file")
            {
                var content = await _indexService.GetContentAsync(isEmpty ? query : "");
                var name = content.Name != null ? content.Name : "";
                return File(content.ContentBytes(), "application/octet-stream", name, true);
            }
            var metaData = await _indexService.GetContentMetasAsync(isEmpty ? query : "");
            return View(metaData);
        }

    }
}
