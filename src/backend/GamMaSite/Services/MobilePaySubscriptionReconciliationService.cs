using System;
using System.Threading;
using System.Threading.Tasks;
using GamMaSite.Data;
using GamMaSite.Models;
using Microsoft.EntityFrameworkCore;

namespace GamMaSite.Services;

/*
 * Reconciles the current MobilePay agreement with our current-state row.
 * The provider call happens before the database transaction; after the
 * response arrives we reload and lock the current row so a stale request
 * cannot overwrite a webhook or cancellation that won the race.
 */
public sealed class MobilePaySubscriptionReconciliationService
{
    private readonly ApplicationDbContext _db;
    private readonly IMobilePayService _mobilePayService;

    public MobilePaySubscriptionReconciliationService(
        ApplicationDbContext db,
        IMobilePayService mobilePayService)
    {
        _db = db;
        _mobilePayService = mobilePayService;
    }

    public async Task<PaymentSubscription> ReconcileAsync(
        PaymentSubscription subscription,
        CancellationToken cancellationToken)
    {
        if (_mobilePayService == null ||
            !string.Equals(subscription.Provider, PaymentOfferProviders.MobilePay, StringComparison.OrdinalIgnoreCase) ||
            !IsOpenStatus(subscription.Status) ||
            string.IsNullOrWhiteSpace(subscription.ProviderSubscriptionId) ||
            subscription.ProviderSubscriptionId.StartsWith("pending:", StringComparison.Ordinal))
        {
            return subscription;
        }

        MobilePayAgreement agreement;
        try
        {
            agreement = await _mobilePayService.GetAgreementAsync(
                subscription.ProviderSubscriptionId,
                cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return subscription;
        }

        var status = agreement.Status?.Trim().ToUpperInvariant() switch
        {
            "ACTIVE" => PaymentSubscriptionStatuses.Active,
            "STOPPED" => PaymentSubscriptionStatuses.Canceled,
            "EXPIRED" or "REJECTED" => PaymentSubscriptionStatuses.Expired,
            _ => null
        };

        if (status == null)
        {
            return subscription;
        }

        await using var transaction = await PaymentWebhookTransaction.BeginAsync(_db, cancellationToken);
        var subscriptionId = subscription.Id;
        _db.Entry(subscription).State = EntityState.Detached;
        var current = await PaymentWebhookTransaction.LockSubscriptionAsync(
            _db,
            subscriptionId,
            cancellationToken);

        if (current == null)
        {
            return subscription;
        }

        if (current.PaymentOfferId == subscription.PaymentOfferId && subscription.PaymentOffer != null)
        {
            current.PaymentOffer = subscription.PaymentOffer;
        }

        // A cancellation already committed locally wins over an older read
        // that happened to receive ACTIVE from the provider.
        if (status == PaymentSubscriptionStatuses.Active && IsTerminalStatus(current.Status))
        {
            await CommitAsync(transaction, cancellationToken);
            return current;
        }

        var changed = false;
        if (!string.Equals(current.Status, status, StringComparison.OrdinalIgnoreCase))
        {
            current.Status = status;
            changed = true;
        }

        if ((status == PaymentSubscriptionStatuses.Canceled ||
                status == PaymentSubscriptionStatuses.Expired) &&
            current.NextChargeAt.HasValue)
        {
            current.NextChargeAt = null;
            changed = true;
        }

        if (status == PaymentSubscriptionStatuses.Active)
        {
            var periodStart = current.CurrentPeriodStart ?? agreement.StartAt ?? agreement.CreatedAt;
            if (periodStart.HasValue)
            {
                if (!current.CurrentPeriodStart.HasValue)
                {
                    current.CurrentPeriodStart = periodStart;
                    changed = true;
                }

                if (!current.LastPaidAt.HasValue)
                {
                    current.LastPaidAt = periodStart;
                    changed = true;
                }

                if (!string.IsNullOrWhiteSpace(current.UserId))
                {
                    var user = await PaymentWebhookTransaction.LockUserAsync(
                        _db,
                        current.UserId,
                        cancellationToken);
                    if (user != null)
                    {
                        var previousStatus = user.Status;
                        var previousMembershipDate = user.KontingentDato;
                        user.MarkAsPayed();
                        if (previousStatus != UserStatus.BETALT ||
                            previousMembershipDate == default ||
                            previousMembershipDate < periodStart.Value)
                        {
                            user.KontingentDato = periodStart.Value;
                        }

                        changed |= previousStatus != user.Status ||
                            previousMembershipDate != user.KontingentDato;
                    }
                }

                var offer = current.PaymentOffer;
                if (offer == null && current.PaymentOfferId.HasValue)
                {
                    offer = await _db.PaymentOffers
                        .FirstOrDefaultAsync(item => item.Id == current.PaymentOfferId, cancellationToken);
                }

                if (offer == null && int.TryParse(current.ProviderProductId, out var offerId))
                {
                    offer = await _db.PaymentOffers
                        .FirstOrDefaultAsync(item => item.Id == offerId, cancellationToken);
                }

                if (offer != null)
                {
                    current.PaymentOffer = offer;
                    var nextChargeAt = GetNextChargeDate(periodStart.Value, offer);
                    if (current.NextChargeAt != nextChargeAt)
                    {
                        current.NextChargeAt = nextChargeAt;
                        changed = true;
                    }
                }
            }
        }

        if (changed)
        {
            current.Updated = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        await CommitAsync(transaction, cancellationToken);
        return current;
    }

    /// <summary>
    /// Closes a pending MobilePay agreement when the user explicitly starts a
    /// new membership checkout. A browser being closed does not produce a
    /// reliable callback, so the old pending agreement must be cleaned up at
    /// the next checkout attempt instead.
    /// </summary>
    public async Task<bool> TryAbandonPendingAsync(
        PaymentSubscription subscription,
        CancellationToken cancellationToken)
    {
        if (subscription == null ||
            !string.Equals(subscription.Provider, PaymentOfferProviders.MobilePay, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(subscription.Status, PaymentSubscriptionStatuses.Pending, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        _db.Entry(subscription).State = EntityState.Detached;

        var providerSubscriptionId = subscription.ProviderSubscriptionId;
        if (string.IsNullOrWhiteSpace(providerSubscriptionId))
        {
            return await MarkTerminalAsync(subscription.Id, PaymentSubscriptionStatuses.Expired, cancellationToken);
        }

        if (providerSubscriptionId.StartsWith("pending:", StringComparison.Ordinal))
        {
            // The provider call never returned an agreement ID, so there is
            // no external agreement to stop.
            return await MarkTerminalAsync(subscription.Id, PaymentSubscriptionStatuses.Expired, cancellationToken);
        }

        if (_mobilePayService == null)
        {
            return false;
        }

        MobilePayAgreement agreement;
        try
        {
            agreement = await _mobilePayService.GetAgreementAsync(
                providerSubscriptionId,
                cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return false;
        }

        var providerStatus = agreement.Status?.Trim().ToUpperInvariant();
        if (providerStatus == "ACTIVE")
        {
            return false;
        }

        if (providerStatus == "CREATED" || providerStatus == "PENDING")
        {
            // MobilePay has not activated the agreement yet. In this state
            // stopping it may itself return 409, but it cannot create future
            // recurring charges. The local pending reservation can therefore
            // safely be released for a new checkout attempt.
            return await MarkTerminalAsync(
                subscription.Id,
                PaymentSubscriptionStatuses.Canceled,
                cancellationToken);
        }

        if (providerStatus != "STOPPED" &&
            providerStatus != "EXPIRED" &&
            providerStatus != "REJECTED")
        {
            try
            {
                await _mobilePayService.StopAgreementAsync(
                    providerSubscriptionId,
                    cancellationToken);
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        var localStatus = providerStatus == "EXPIRED" || providerStatus == "REJECTED"
            ? PaymentSubscriptionStatuses.Expired
            : PaymentSubscriptionStatuses.Canceled;
        return await MarkTerminalAsync(subscription.Id, localStatus, cancellationToken);
    }

    private static async Task CommitAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        if (transaction != null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
    }

    private async Task<bool> MarkTerminalAsync(
        int subscriptionId,
        string status,
        CancellationToken cancellationToken)
    {
        await using var transaction = await PaymentWebhookTransaction.BeginAsync(_db, cancellationToken);
        var current = await PaymentWebhookTransaction.LockSubscriptionAsync(
            _db,
            subscriptionId,
            cancellationToken);

        if (current == null)
        {
            return false;
        }

        if (!string.Equals(current.Status, PaymentSubscriptionStatuses.Pending, StringComparison.OrdinalIgnoreCase))
        {
            await CommitAsync(transaction, cancellationToken);
            return IsTerminalStatus(current.Status);
        }

        current.Status = status;
        current.NextChargeAt = null;
        current.Updated = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await CommitAsync(transaction, cancellationToken);
        return true;
    }

    private static DateTime GetNextChargeDate(DateTime paidAt, PaymentOffer offer)
    {
        var interval = string.IsNullOrWhiteSpace(offer.BillingInterval)
            ? "YEAR"
            : offer.BillingInterval.Trim().ToUpperInvariant();
        var count = offer.BillingIntervalCount.GetValueOrDefault(1);
        if (interval == "YEAR")
        {
            return new DateTime(
                paidAt.Year + Math.Max(count, 1),
                2,
                1,
                0,
                0,
                0,
                DateTimeKind.Utc);
        }

        return interval switch
        {
            "DAY" => paidAt.AddDays(count),
            "WEEK" => paidAt.AddDays(count * 7),
            "MONTH" => paidAt.AddMonths(count),
            _ => paidAt.AddYears(count)
        };
    }

    private static bool IsOpenStatus(string status)
    {
        return status == PaymentSubscriptionStatuses.Pending ||
            status == PaymentSubscriptionStatuses.Active ||
            status == PaymentSubscriptionStatuses.Trialing ||
            status == PaymentSubscriptionStatuses.PastDue ||
            status == PaymentSubscriptionStatuses.Incomplete;
    }

    private static bool IsTerminalStatus(string status)
    {
        return string.Equals(status, PaymentSubscriptionStatuses.Canceled, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, PaymentSubscriptionStatuses.Expired, StringComparison.OrdinalIgnoreCase);
    }
}
