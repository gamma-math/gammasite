using System;
using System.Collections.Generic;

namespace GamMaSite.ViewModels.Api
{
    /*
     * DTOs for member directory, membership status updates, and role assignment.
     */
    public class MemberDto
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public int GraduationYear { get; set; }

        public string Occupation { get; set; }

        public string Email { get; set; }

        public string PhoneNumber { get; set; }

        public string Status { get; set; }

        public bool IsVisible { get; set; }

        public DateTime MembershipPaidAt { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class UpdateMemberStatusRequest
    {
        public string Status { get; set; }
    }

    public class MassUpdateMemberStatusRequest
    {
        public DateTime From { get; set; }

        public DateTime To { get; set; }

        public string Status { get; set; }
    }

    public class MassUpdateMemberStatusResult
    {
        public int Updated { get; set; }
    }

    public class RoleDto
    {
        public string Id { get; set; }

        public string Name { get; set; }
    }

    public class SaveRoleRequest
    {
        public string Name { get; set; }
    }

    public class RoleMembersDto
    {
        public RoleDto Role { get; set; }

        public IReadOnlyList<MemberDto> Members { get; set; }

        public IReadOnlyList<MemberDto> NonMembers { get; set; }
    }

    public class UpdateRoleMembersRequest
    {
        public string[] AddIds { get; set; }

        public string[] DeleteIds { get; set; }
    }
}
