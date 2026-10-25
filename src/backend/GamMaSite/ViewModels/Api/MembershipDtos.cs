using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

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

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? HasActiveSubscription { get; set; }
    }

    public class FinanceMemberDto
    {
        public string Id { get; set; }

        public string Name { get; set; }
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

        public bool ExcludeActiveSubscriptions { get; set; }
    }

    public class MassUpdateMemberStatusResult
    {
        public int Updated { get; set; }
    }

    public class AdminSubscriptionDto
    {
        public string ProviderSubscriptionId { get; set; }

        public string Provider { get; set; }

        public string ProductName { get; set; }

        public string ProviderProductId { get; set; }

        public string ProviderPriceId { get; set; }

        public string ProviderDashboardUrl { get; set; }

        public string Status { get; set; }

        public DateTime? CurrentPeriodStart { get; set; }

        public DateTime? CurrentPeriodEnd { get; set; }

        public DateTime? NextChargeAt { get; set; }

        public DateTime? LastPaidAt { get; set; }

        public bool CancelAtPeriodEnd { get; set; }

        public DateTime Created { get; set; }

        public DateTime Updated { get; set; }
    }

    public class AdminMemberSubscriptionsDto
    {
        public string MemberName { get; set; }

        public string MemberEmail { get; set; }

        public IReadOnlyList<AdminSubscriptionDto> Subscriptions { get; set; }
    }

    public class AdminPaymentSubscriptionDto
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string Provider { get; set; }
        public string ProviderSubscriptionId { get; set; }
        public int? PaymentOfferId { get; set; }
        public string ProviderProductId { get; set; }
        public string ProviderPriceId { get; set; }
        public string ProductName { get; set; }
        public long? AmountMinor { get; set; }
        public string Currency { get; set; }
        public string OfferType { get; set; }
        public string Scope { get; set; }
        public string Status { get; set; }
        public DateTime? CurrentPeriodStart { get; set; }
        public DateTime? CurrentPeriodEnd { get; set; }
        public DateTime? LastPaidAt { get; set; }
        public DateTime? NextChargeAt { get; set; }
        public bool CancelAtPeriodEnd { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
        public string LastProcessedWebhookId { get; set; }
        public DateTime? LastProcessedWebhookAt { get; set; }
    }

    public class AdminSubscriptionPaymentDto
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public long AmountMinor { get; set; }
        public string Currency { get; set; }
        public string Description { get; set; }
        public DateTime? DueAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public string TransactionId { get; set; }
        public string FailureReason { get; set; }
    }

    public class AdminPaymentSubscriptionDetailDto
    {
        public AdminPaymentSubscriptionDto Subscription { get; set; }
        public IReadOnlyList<AdminSubscriptionPaymentDto> Payments { get; set; }
        public string PaymentsError { get; set; }
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

    public class PermissionDto
    {
        public int Id { get; set; }

        public string Code { get; set; }

        public string Description { get; set; }

        public bool Enabled { get; set; }
    }

    public class RolePermissionsDto
    {
        public RoleDto Role { get; set; }

        public IReadOnlyList<PermissionDto> Permissions { get; set; }
    }

    public class UpdateRolePermissionsRequest
    {
        public string[] PermissionCodes { get; set; }
    }
}
