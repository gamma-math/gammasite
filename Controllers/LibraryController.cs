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

        // MVC action — kept until the React page is cut over
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

        // REST endpoint for the React SPA: returns folder entries as JSON.
        // File downloads are handled by the browser via /library/download?path=…
        // so the browser can prompt a Save dialog natively without JS blob handling.
        [HttpGet("/api/library")]
        public async Task<IActionResult> GetFolder([FromQuery] string path = "")
        {
            var metas = await _indexService.GetContentMetasAsync(path);

            // If the result is empty the path points to a file — tell the SPA to
            // navigate the browser directly to the download endpoint instead.
            if (metas.Count == 0)
                return Ok(new { isFile = true, downloadUrl = $"/library/download?path={Uri.EscapeDataString(path)}" });

            var wrapper = new ContentMetas { Metas = metas };
            return Ok(new
            {
                isFile = false,
                root = wrapper.GetRoot(),
                parent = wrapper.HasParent() ? wrapper.GetParent() : (string?)null,
                entries = metas.Select(m => new
                {
                    name = m.Name,
                    path = m.Path,
                    type = m.Type,
                    icon = m.TypeConverted(),
                    isFile = new[] { "file", "blob" }.Contains(m.Type),
                }),
            });
        }

        // Serves a raw file for download — called directly by the browser, not via fetch.
        [HttpGet("/library/download")]
        public async Task<IActionResult> Download([FromQuery] string path)
        {
            if (string.IsNullOrEmpty(path)) return BadRequest();
            var content = await _indexService.GetContentAsync(path);
            return File(content.Content, content.MimeType);
        }
    }
}
