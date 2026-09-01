using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GamMaSite.Services;
using GamMaSite.ViewModels.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GamMaSite.Controllers
{
    [ApiController]
    [Route("api/content")]
    public class ApiContentController : ControllerBase
    {
        private const string AdminRoles = "Admin,ADMIN";
        private const string ReadAdminRoles = "Admin,ADMIN,READ_ADMIN";

        private readonly IContentService _contentService;
        private readonly IEventRegistrationService _registrationService;

        public ApiContentController(IContentService contentService, IEventRegistrationService registrationService)
        {
            _contentService = contentService;
            _registrationService = registrationService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublished([FromQuery] string type)
        {
            var items = await _contentService.GetPublishedAsync(type);
            return Ok(items.Select(item => item.ToDto()));
        }

        [HttpGet("admin")]
        [Authorize(Roles = ReadAdminRoles)]
        public async Task<IActionResult> GetAll([FromQuery] string type, [FromQuery] string status)
        {
            var items = await _contentService.GetAllAsync(type, status);
            return Ok(items.Select(item => item.ToDto()));
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _contentService.GetByIdAsync(id, UserCanReadUnpublished());
            return item == null ? NotFound() : Ok(item.ToDto());
        }

        [HttpGet("slug/{slug}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetBySlug(string slug)
        {
            var item = await _contentService.GetBySlugAsync(slug, UserCanReadUnpublished());
            return item == null ? NotFound() : Ok(item.ToDto());
        }

        [HttpPost]
        [Authorize(Roles = AdminRoles)]
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
        [Authorize(Roles = AdminRoles)]
        public async Task<IActionResult> Update(int id, SaveContentItemRequest request)
        {
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
        [Authorize(Roles = AdminRoles)]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _contentService.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }

        [HttpPost("{id:int}/registrations")]
        [Authorize]
        public async Task<IActionResult> Register(int id, SaveEventRegistrationRequest request)
        {
            try
            {
                var registration = await _registrationService.RegisterAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier), request);
                return Ok(registration.ToDto());
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{id:int}/registrations/admin")]
        [Authorize(Roles = AdminRoles)]
        public async Task<IActionResult> AddRegistration(int id, AddEventRegistrationRequest request)
        {
            try
            {
                var registration = await _registrationService.AddAsync(id, request);
                return Ok(registration.ToDto());
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
            var deleted = await _registrationService.UnregisterAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier));
            return deleted ? NoContent() : NotFound();
        }

        [HttpGet("{id:int}/registrations/me")]
        [Authorize]
        public async Task<IActionResult> GetMyRegistration(int id)
        {
            var registration = await _registrationService.GetRegistrationAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier));
            return registration == null ? NotFound() : Ok(registration.ToDto());
        }

        [HttpGet("{id:int}/registrations")]
        [Authorize]
        public async Task<IActionResult> GetRegistrations(int id)
        {
            var registrations = await _registrationService.GetRegistrationsAsync(id);
            return Ok(registrations.Select(registration => registration.ToDto()));
        }

        [HttpPut("{id:int}/registrations/{registrationId:int}")]
        [Authorize(Roles = AdminRoles)]
        public async Task<IActionResult> UpdateRegistration(int id, int registrationId, UpdateEventRegistrationRequest request)
        {
            try
            {
                var registration = await _registrationService.UpdateAsync(id, registrationId, request);
                return registration == null ? NotFound() : Ok(registration.ToDto());
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        private bool UserCanReadUnpublished()
        {
            return User.IsInRole("Admin") || User.IsInRole("ADMIN") || User.IsInRole("READ_ADMIN");
        }
    }
}
