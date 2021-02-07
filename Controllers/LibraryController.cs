﻿using System;
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

        public async Task<IActionResult> IndexAsync(string query)
        {
            var isEmpty = !string.IsNullOrEmpty(query);
            if (_indexService.isFileName(query))
            {
                var content = await _indexService.GetContentAsync(isEmpty ? query : "");
                var name = content.Name != null ? content.Name : "";
                var splitted = name.Split(".");
                var mimeType = MimeTypeMap.GetMimeType(splitted.Length > 1 ? splitted[1] : "yml");
                if (mimeType != "application/octet-stream")
                {
                    return File(content.ContentBytes(), mimeType);
                }
                return File(content.ContentBytes(), "text/plain");
            }
            var metaData = await _indexService.GetContentMetasAsync(isEmpty ? query : "");
            return View(metaData);
        }

    }
}
