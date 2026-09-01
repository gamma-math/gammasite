using System.Linq;
using System.Threading.Tasks;
using GamMaSite.Services;
using GamMaSite.ViewModels.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GamMaSite.Controllers
{
    [ApiController]
    [Route("api/library")]
    [Authorize]
    public class ApiLibraryController : ControllerBase
    {
        private readonly IIndexService _indexService;

        public ApiLibraryController(IIndexService indexService)
        {
            _indexService = indexService;
        }

        [HttpGet]
        public async Task<IActionResult> GetListing([FromQuery] string path)
        {
            var metas = new ContentMetas
            {
                Metas = await _indexService.GetContentMetasAsync(path ?? string.Empty)
            };

            return Ok(new LibraryListingDto
            {
                Root = metas.GetRoot(),
                Parent = metas.GetParent(),
                HasParent = metas.HasParent(),
                Items = metas.Metas.Select(item => new LibraryItemDto
                {
                    Name = item.Name,
                    Path = item.Path,
                    Type = item.Type,
                    Icon = item.TypeConverted()
                }).ToList()
            });
        }
    }
}
