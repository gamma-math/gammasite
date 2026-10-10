using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GamMaSite.Models;
using GamMaSite.Services;
using GamMaSite.ViewModels.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace GamMaSite.Controllers
{
    [ApiController]
    [Route("api/content")]
    [AutoValidateAntiforgeryToken]
    /*
     * Provides React with CRUD and publishing endpoints for events, news, and related links.
     */
    public class ApiContentController : ControllerBase
    {
        private readonly IContentService _contentService;
        private readonly IEventRegistrationService _registrationService;
        private readonly IAccessControlService _accessControl;
        private readonly IContentMediaService _contentMedia;

        public ApiContentController(
            IContentService contentService,
            IEventRegistrationService registrationService,
            IAccessControlService accessControl,
            IContentMediaService contentMedia = null)
        {
            _contentService = contentService;
            _registrationService = registrationService;
            _accessControl = accessControl;
            _contentMedia = contentMedia;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublished([FromQuery] string type, [FromQuery] bool frontPage = false)
        {
            var items = await _contentService.GetPublishedAsync(type, frontPage);
            return Ok(items.Select(item => item.ToDto()));
        }

        [HttpGet("admin")]
        [Authorize(Policy = PermissionPolicies.ContentOrMessages)]
        public async Task<IActionResult> GetAll([FromQuery] string type, [FromQuery] string status)
        {
            var items = await _contentService.GetAllAsync(type, status);
            return Ok(items.Select(item => item.ToDto()));
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _contentService.GetByIdAsync(id, await _accessControl.CanEditEventAsync(User, id));
            return item == null ? NotFound() : Ok(item.ToDto());
        }

        [HttpGet("{id:int}/access")]
        [Authorize]
        public async Task<IActionResult> GetAccess(int id)
        {
            var isOrganizer = await _accessControl.IsEventOrganizerAsync(User, id);
            var canEditContent = await _accessControl.HasPermissionAsync(User, PermissionCodes.ContentEdit);
            var canEditRegistrations = canEditContent || isOrganizer || await _accessControl.HasPermissionAsync(User, PermissionCodes.RegistrationsEdit);

            return Ok(new
            {
                canEditEvent = canEditContent || isOrganizer,
                canEditRegistrations,
                isOrganizer
            });
        }

        [HttpGet("slug/{slug}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetBySlug(string slug)
        {
            var item = await _contentService.GetBySlugAsync(slug, await UserCanReadUnpublished());
            return item == null ? NotFound() : Ok(item.ToDto());
        }

        [HttpPost]
        [Authorize(Policy = PermissionPolicies.ContentEdit)]
        public async Task<IActionResult> Create(SaveContentItemRequest request)
        {
            try
            {
                var item = await _contentService.CreateAsync(request, User.FindFirstValue(ClaimTypes.NameIdentifier));
                return CreatedAtAction(nameof(GetById), new { id = item.Id }, item.ToDto());
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, SaveContentItemRequest request)
        {
            if (!await _accessControl.CanEditEventAsync(User, id))
            {
                return Forbid();
            }

            if (!await _accessControl.HasPermissionAsync(User, PermissionCodes.ContentEdit) &&
                !string.Equals(request?.Type, ContentTypes.Event, StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            try
            {
                var item = await _contentService.UpdateAsync(id, request);
                return item == null ? NotFound() : Ok(item.ToDto());
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = PermissionPolicies.ContentEdit)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var deleted = await _contentService.DeleteAsync(id);
                return deleted ? NoContent() : NotFound();
            }
            catch (ContentMediaStorageException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message });
            }
        }

        [HttpPost("{id:int}/image")]
        [Authorize]
        [RequestSizeLimit(ContentMediaService.MaxImageSize + 128 * 1024)]
        public async Task<IActionResult> UploadImage(int id, IFormFile file)
        {
            var item = await _contentService.GetByIdAsync(id, true);
            if (item == null)
            {
                return NotFound();
            }

            if (!await CanEditContentMediaAsync(item))
            {
                return Forbid();
            }

            try
            {
                if (_contentMedia == null)
                {
                    return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Billedelagring er ikke konfigureret." });
                }

                return Ok(await _contentMedia.UploadAsync(id, file));
            }
            catch (ContentMediaValidationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (ContentMediaStorageException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message });
            }
        }

        [HttpDelete("{id:int}/image")]
        [Authorize]
        public async Task<IActionResult> DeleteImage(int id)
        {
            var item = await _contentService.GetByIdAsync(id, true);
            if (item == null)
            {
                return NotFound();
            }

            if (!await CanEditContentMediaAsync(item))
            {
                return Forbid();
            }

            try
            {
                if (_contentMedia == null)
                {
                    return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Billedelagring er ikke konfigureret." });
                }

                var result = await _contentMedia.DeleteAsync(id);
                return result == null ? NotFound() : Ok(result);
            }
            catch (ContentMediaStorageException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message });
            }
        }

        [HttpPost("{id:int}/registrations")]
        [Authorize]
        public async Task<IActionResult> Register(int id, SaveEventRegistrationRequest request)
        {
            try
            {
                var registration = await _registrationService.RegisterAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier), request);
                return Ok(registration.ToDto(await _accessControl.HasAnyPermissionAsync(User, PermissionCodes.ContentEdit, PermissionCodes.RegistrationsEdit)));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{id:int}/registrations/admin")]
        [Authorize]
        public async Task<IActionResult> AddRegistration(int id, AddEventRegistrationRequest request)
        {
            if (!await _accessControl.CanEditRegistrationsAsync(User, id))
            {
                return Forbid();
            }

            try
            {
                var registration = await _registrationService.AddAsync(id, request);
                return Ok(registration.ToDto(true));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{id:int}/registrations/me")]
        [Authorize]
        public async Task<IActionResult> Unregister(int id)
        {
            try
            {
                var deleted = await _registrationService.UnregisterAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier));
                return deleted ? NoContent() : NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{id:int}/registrations/{registrationId:int}")]
        [Authorize]
        public async Task<IActionResult> DeleteRegistration(int id, int registrationId)
        {
            if (!await _accessControl.CanEditRegistrationsAsync(User, id))
            {
                return Forbid();
            }

            try
            {
                var deleted = await _registrationService.DeleteAsync(id, registrationId);
                return deleted ? NoContent() : NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{id:int}/registrations/me")]
        [Authorize]
        public async Task<IActionResult> GetMyRegistration(int id)
        {
            var registration = await _registrationService.GetRegistrationAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier));
            return registration == null ? NoContent() : Ok(registration.ToDto(await _accessControl.HasAnyPermissionAsync(User, PermissionCodes.ContentEdit, PermissionCodes.RegistrationsEdit)));
        }

        [HttpGet("{id:int}/registrations")]
        [Authorize]
        public async Task<IActionResult> GetRegistrations(int id)
        {
            var registrations = await _registrationService.GetRegistrationsAsync(id);
            var includePrivateDetails = await _accessControl.CanEditRegistrationsAsync(User, id);
            return Ok(registrations.Select(registration => registration.ToDto(includePrivateDetails)));
        }

        [HttpPut("{id:int}/registrations/{registrationId:int}")]
        [Authorize]
        public async Task<IActionResult> UpdateRegistration(int id, int registrationId, UpdateEventRegistrationRequest request)
        {
            if (!await _accessControl.CanEditRegistrationsAsync(User, id))
            {
                return Forbid();
            }

            try
            {
                var registration = await _registrationService.UpdateAsync(id, registrationId, request);
                return registration == null ? NotFound() : Ok(registration.ToDto(true));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        private Task<bool> UserCanReadUnpublished()
        {
            return _accessControl.HasPermissionAsync(User, PermissionCodes.ContentEdit);
        }

        private Task<bool> CanEditContentMediaAsync(ContentItem item)
        {
            return string.Equals(item.Type, ContentTypes.Event, StringComparison.OrdinalIgnoreCase)
                ? _accessControl.CanEditEventAsync(User, item.Id)
                : _accessControl.HasPermissionAsync(User, PermissionCodes.ContentEdit);
        }
    }
}
