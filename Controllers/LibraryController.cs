using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GamMaSite.Models;
using MimeTypes;
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
            var isEmpty = !string.IsNullOrEmpty(path);
            var metaData = await _indexService.GetContentMetasAsync(isEmpty ? path : "");
            if (metaData.Count == 0)
            {
                var content = await _indexService.GetContentAsync(isEmpty ? path : "");
                var name = content.Name != null ? content.Name : "";
                var splitted = name.Split(".");
                var mimeType = MimeTypeMap.GetMimeType(splitted.Length > 1 ? splitted.Last() : "txt");
                if (new string[] { "text/plain", "application/octet-stream" }.Contains(mimeType))
                {
                    mimeType = "text/plain;charset=utf-8";
                }
                return File(content.ContentBytes(), mimeType);
            }
            return View(metaData);
        }

    }
}
