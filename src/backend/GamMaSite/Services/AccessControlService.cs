using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GamMaSite.Data;
using GamMaSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GamMaSite.Services
{
    public static class PermissionCodes
    {
        public const string ContentEdit = "content.edit";
        public const string RegistrationsEdit = "registrations.edit";
        public const string EmailTemplatesEdit = "email_templates.edit";
        public const string MessagesEdit = "messages.edit";
        public const string RolesEdit = "roles.edit";
        public const string FinanceViewAll = "finance.view.all";
        public const string FinanceEditAll = "finance.edit.all";
    }

    public static class PermissionPolicies
    {
        public const string ContentEdit = "permission.content.edit";
        public const string ContentOrMessages = "permission.content-or-messages";
        public const string ContentOrRegistrations = "permission.content-or-registrations";
        public const string ContentOrEmailTemplates = "permission.content-or-email-templates";
        public const string EmailTemplatesOrMessages = "permission.email-templates-or-messages";
        public const string ContentOrEmailTemplatesOrMessages = "permission.content-or-email-templates-or-messages";
        public const string MemberData = "permission.member-data";
        public const string FinanceMemberData = "permission.finance-member-data";
        public const string RegistrationsEdit = "permission.registrations.edit";
        public const string EmailTemplatesEdit = "permission.email-templates.edit";
        public const string MessagesEdit = "permission.messages.edit";
        public const string RolesEdit = "permission.roles.edit";
        public const string RolesOrMessages = "permission.roles-or-messages";
        public const string FinanceViewAll = "permission.finance.view.all";
        public const string FinanceEditAll = "permission.finance.edit.all";
    }

    public sealed class PermissionRequirement : IAuthorizationRequirement
    {
        public PermissionRequirement(params string[] codes)
        {
            Codes = codes ?? Array.Empty<string>();
        }

        public IReadOnlyCollection<string> Codes { get; }
    }

    public interface IAccessControlService
    {
        Task<bool> HasPermissionAsync(ClaimsPrincipal user, string permissionCode);

        Task<bool> HasAnyPermissionAsync(ClaimsPrincipal user, params string[] permissionCodes);

        Task<IReadOnlyList<string>> GetPermissionsAsync(ClaimsPrincipal user);

        Task<bool> IsEventOrganizerAsync(ClaimsPrincipal user, int contentItemId);

        Task<bool> CanEditEventAsync(ClaimsPrincipal user, int contentItemId);

        Task<bool> CanEditRegistrationsAsync(ClaimsPrincipal user, int contentItemId);
    }

    public sealed class AccessControlService : IAccessControlService
    {
        private readonly ApplicationDbContext _db;

        public AccessControlService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<bool> HasPermissionAsync(ClaimsPrincipal user, string permissionCode)
        {
            return await HasAnyPermissionAsync(user, permissionCode);
        }

        public async Task<bool> HasAnyPermissionAsync(ClaimsPrincipal user, params string[] permissionCodes)
        {
            var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier);
            var codes = permissionCodes?.Where(code => !string.IsNullOrWhiteSpace(code)).ToArray() ?? Array.Empty<string>();
            if (string.IsNullOrWhiteSpace(userId) || codes.Length == 0)
            {
                return false;
            }

            return await _db.UserRoles
                .Where(userRole => userRole.UserId == userId)
                .Join(_db.RolePermissions, userRole => userRole.RoleId, rolePermission => rolePermission.RoleId, (_, rolePermission) => rolePermission.PermissionId)
                .Join(_db.Permissions, permissionId => permissionId, permission => permission.Id, (_, permission) => permission.Code)
                .AnyAsync(code => System.Linq.Enumerable.Contains(codes, code));
        }

        public async Task<IReadOnlyList<string>> GetPermissionsAsync(ClaimsPrincipal user)
        {
            var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Array.Empty<string>();
            }

            return await _db.UserRoles
                .Where(userRole => userRole.UserId == userId)
                .Join(_db.RolePermissions, userRole => userRole.RoleId, rolePermission => rolePermission.RoleId, (_, rolePermission) => rolePermission.PermissionId)
                .Join(_db.Permissions, permissionId => permissionId, permission => permission.Id, (_, permission) => permission.Code)
                .Distinct()
                .OrderBy(code => code)
                .ToListAsync();
        }

        public async Task<bool> IsEventOrganizerAsync(ClaimsPrincipal user, int contentItemId)
        {
            var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return false;
            }

            return await _db.EventRegistrations
                .AnyAsync(registration =>
                    registration.ContentItemId == contentItemId &&
                    registration.UserId == userId &&
                    registration.RegistrationType == RegistrationTypes.Organizer &&
                    registration.Registered &&
                    registration.ContentItem.Type == ContentTypes.Event);
        }

        public async Task<bool> CanEditEventAsync(ClaimsPrincipal user, int contentItemId)
        {
            return await HasPermissionAsync(user, PermissionCodes.ContentEdit)
                || await IsEventOrganizerAsync(user, contentItemId);
        }

        public async Task<bool> CanEditRegistrationsAsync(ClaimsPrincipal user, int contentItemId)
        {
            return await HasAnyPermissionAsync(user, PermissionCodes.ContentEdit, PermissionCodes.RegistrationsEdit)
                || await IsEventOrganizerAsync(user, contentItemId);
        }
    }

    public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IAccessControlService _accessControl;

        public PermissionAuthorizationHandler(IAccessControlService accessControl)
        {
            _accessControl = accessControl;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            if (await _accessControl.HasAnyPermissionAsync(context.User, requirement.Codes.ToArray()))
            {
                context.Succeed(requirement);
            }
        }
    }
}
