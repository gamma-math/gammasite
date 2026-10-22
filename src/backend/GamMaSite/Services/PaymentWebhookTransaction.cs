using System.Threading;
using System.Threading.Tasks;
using GamMaSite.Data;
using GamMaSite.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GamMaSite.Services;

/*
 * Keeps webhook state transitions serialized without adding a payment-event log.
 * The production database is MySQL, so FOR UPDATE gives us a row lock for the
 * existing current-state records. The InMemory provider used by unit tests does
 * not support transactions or raw SQL and therefore uses the normal query path.
 */
public static class PaymentWebhookTransaction
{
    public static Task<IDbContextTransaction> BeginAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        return db.Database.IsRelational()
            ? db.Database.BeginTransactionAsync(cancellationToken)
            : Task.FromResult<IDbContextTransaction>(null);
    }

    public static Task<PaymentSubscription> LockSubscriptionAsync(
        ApplicationDbContext db,
        int id,
        CancellationToken cancellationToken)
    {
        if (!IsMySql(db))
        {
            return db.PaymentSubscriptions.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        return db.PaymentSubscriptions
            .FromSqlInterpolated($"SELECT * FROM `PaymentSubscriptions` WHERE `Id` = {id} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
    }

    public static Task<EventRegistration> LockRegistrationAsync(
        ApplicationDbContext db,
        int id,
        CancellationToken cancellationToken)
    {
        if (!IsMySql(db))
        {
            return db.EventRegistrations.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        return db.EventRegistrations
            .FromSqlInterpolated($"SELECT * FROM `EventRegistrations` WHERE `Id` = {id} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
    }

    public static Task<SiteUser> LockUserAsync(
        ApplicationDbContext db,
        string id,
        CancellationToken cancellationToken)
    {
        if (!IsMySql(db))
        {
            return db.Users.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        return db.Users
            .FromSqlInterpolated($"SELECT * FROM `AspNetUsers` WHERE `Id` = {id} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static bool IsMySql(ApplicationDbContext db)
    {
        return db.Database.ProviderName?.Contains("MySql", System.StringComparison.OrdinalIgnoreCase) == true;
    }
}
