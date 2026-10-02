using System;
using System.Linq;
using System.Threading.Tasks;
using GamMaSite.Services;
using GamMaSite.ViewModels.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GamMaSite.Controllers
{
    [ApiController]
    [Route("api/email-templates")]
    [Authorize]
    [AutoValidateAntiforgeryToken]
    /*
     * Provides React admin endpoints for managing reusable email templates.
     */
    public class ApiEmailTemplatesController : ControllerBase
    {
        private readonly IEmailTemplateService _emailTemplateService;

        public ApiEmailTemplatesController(IEmailTemplateService emailTemplateService)
        {
            _emailTemplateService = emailTemplateService;
        }

        [HttpGet]
        [Authorize(Policy = PermissionPolicies.EmailTemplatesOrMessages)]
        public async Task<IActionResult> GetAll([FromQuery] string templateType, [FromQuery] bool? isActive)
        {
            var templates = await _emailTemplateService.GetAllAsync(templateType, isActive);
            return Ok(templates.Select(template => template.ToDto()));
        }

        [HttpGet("{id:int}")]
        [Authorize(Policy = PermissionPolicies.EmailTemplatesOrMessages)]
        public async Task<IActionResult> GetById(int id)
        {
            var template = await _emailTemplateService.GetByIdAsync(id);
            return template == null ? NotFound() : Ok(template.ToDto());
        }

        [HttpPost]
        [Authorize(Policy = PermissionPolicies.EmailTemplatesEdit)]
        public async Task<IActionResult> Create(SaveEmailTemplateRequest request)
        {
            try
            {
                var template = await _emailTemplateService.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = template.Id }, template.ToDto());
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = PermissionPolicies.EmailTemplatesEdit)]
        public async Task<IActionResult> Update(int id, SaveEmailTemplateRequest request)
        {
            try
            {
                var template = await _emailTemplateService.UpdateAsync(id, request);
                return template == null ? NotFound() : Ok(template.ToDto());
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = PermissionPolicies.EmailTemplatesEdit)]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _emailTemplateService.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }

        [HttpPost("{id:int}/preview")]
        [Authorize(Policy = PermissionPolicies.EmailTemplatesOrMessages)]
        public async Task<IActionResult> Preview(int id, PreviewEmailTemplateRequest request)
        {
            var preview = await _emailTemplateService.PreviewAsync(id, request);
            return preview == null ? NotFound() : Ok(preview);
        }
    }
}
