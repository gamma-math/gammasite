using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GamMaSite.Controllers
{
    [ApiController]
    [Route("api/editor")]
    [Authorize(Roles = "Admin,ADMIN")]
    [AutoValidateAntiforgeryToken]
    /* Stores images uploaded from the React rich text editors. */
    public class ApiEditorController : ControllerBase
    {
        private const long MaxImageSize = 10 * 1024 * 1024;
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp"
        };
        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/gif", "image/webp"
        };

        private readonly IWebHostEnvironment _environment;

        public ApiEditorController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        /* Validates and stores one editor image in the public uploads folder. */
        [HttpPost("images")]
        [RequestSizeLimit(MaxImageSize)]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "Vælg et billede først." });
            }

            var extension = Path.GetExtension(file.FileName);
            if (file.Length > MaxImageSize || !AllowedExtensions.Contains(extension) || !AllowedContentTypes.Contains(file.ContentType))
            {
                return BadRequest(new { error = "Billedet skal være JPG, PNG, GIF eller WEBP og højst 10 MB." });
            }

            var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            var directory = Path.Combine(webRoot, "uploads", "editor");
            Directory.CreateDirectory(directory);

            var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var path = Path.Combine(directory, fileName);
            await using (var stream = System.IO.File.Create(path))
            {
                await file.CopyToAsync(stream);
            }

            var publicUrl = $"{Request.Scheme}://{Request.Host}/uploads/editor/{fileName}";
            return Ok(new { url = publicUrl });
        }
    }
}
