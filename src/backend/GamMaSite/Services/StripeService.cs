using Stripe;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GamMaSite.Services
{
    /*
     * Defines Stripe product, price, session, and checkout operations.
     */
    public interface IStripeService
    {
        string StartPayment(Product product, Price price, string user, string successUrl, string cancelUrl, string mode = "payment", IDictionary<string, string> metadata = null, string idempotencyKey = null);

        bool IsPaymentComplete(Session session);

        Task<Session> GetSessionAsync(string sessionId);

        Task<IReadOnlyList<StripeBalanceTransaction>> GetPayoutBalanceTransactionsAsync(
            string payoutId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<StripeBalanceTransaction>> GetAvailablePaymentBalanceTransactionsAsync(
            DateTime createdFrom,
            DateTime createdToExclusive,
            CancellationToken cancellationToken = default);

        Task<Product[]> GetAllProductsAsync();

        Task<Product> GetProductAsync(string id);

        Task<Product> GetProductByExactNameAsync(string name);

        Task<Product> CreateProductAsync(string name, string description);

        Task<bool> DeactivateProductAsync(string id);

        Task<bool> SetProductActiveAsync(string id, bool active);

        Task<Price> CreateOneTimePriceAsync(string productId, long amountMinor, string currency, string nickname = null);

        Task<Price> CreateRecurringPriceAsync(string productId, long amountMinor, string currency, string interval, int intervalCount, string nickname = null);

        Task<Price> UpdatePriceNicknameAsync(string id, string nickname);

        Task<bool> DeactivatePriceAsync(string id);

        Task<bool> SetPriceActiveAsync(string id, bool active);

        Task<Price> GetPriceAsync(string product);

        Task<Price> GetPriceByIdAsync(string id);

        Task<Subscription> GetSubscriptionAsync(string id);

        Task<IReadOnlyList<StripeSubscriptionPayment>> GetSubscriptionPaymentsAsync(string subscriptionId);

        Task<bool> UpdateSubscriptionPriceAsync(string subscriptionId, string priceId);

        Task<Subscription> CancelSubscriptionAsync(string id);
    }

    /*
     * Wraps Stripe SDK calls used by the PaymentOffers API and webhooks.
     */
    public sealed record StripeSubscriptionPayment(
        string Id,
        string Status,
        long AmountMinor,
        string Currency,
        string Description,
        DateTime CreatedAt,
        DateTime? DueAt,
        DateTime? PaidAt);

    public sealed record StripeBalanceTransaction(
        string BalanceTransactionId,
        string PayoutId,
        DateTime Date,
        DateTime AvailableDate,
        DateTime? PayoutDate,
        decimal Amount,
        decimal Fee,
        decimal Net,
        string Currency,
        string TransactionType,
        string ReportingCategory,
        string SourceId,
        string PaymentIntentId,
        string Description,
        string UserId,
        string PayerName,
        string ReceiptUrl);

    public class StripeService : IStripeService
    {
        public static bool IsPaymentTransaction(string transactionType, string reportingCategory)
        {
            if (!string.IsNullOrWhiteSpace(reportingCategory))
            {
                return string.Equals(reportingCategory, "charge", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(reportingCategory, "payment", StringComparison.OrdinalIgnoreCase);
            }

            return string.Equals(transactionType, "charge", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(transactionType, "payment", StringComparison.OrdinalIgnoreCase);
        }

        public string StartPayment(Product product, Price price, string user, string successUrl, string cancelUrl, string mode = "payment", IDictionary<string, string> metadata = null, string idempotencyKey = null)
        {
            Session session = GetCreateSession(price.Id, product.Id, product.Name, user, successUrl, cancelUrl, mode, metadata, idempotencyKey);

            return session.Id;
        }

        public bool IsPaymentComplete(Session session)
        {
            var paymentComplete = new[] { "paid", "no_payment_required" }.Contains(session?.PaymentStatus);
            return paymentComplete;
        }

        public async Task<Session> GetSessionAsync(string sessionId)
        {
            try
            {
                var session = await new SessionService().GetAsync(sessionId);
                return session;
            }
            catch (StripeException)
            {
                return null;
            }
        }

        public async Task<IReadOnlyList<StripeBalanceTransaction>> GetPayoutBalanceTransactionsAsync(
            string payoutId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(payoutId))
            {
                return Array.Empty<StripeBalanceTransaction>();
            }

            var payout = await new PayoutService().GetAsync(payoutId, cancellationToken: cancellationToken);
            var balanceTransactions = new BalanceTransactionService();
            var options = new BalanceTransactionListOptions
            {
                Payout = payoutId,
                Limit = 100
            };
            var result = new List<StripeBalanceTransaction>();

            await foreach (var transaction in balanceTransactions.ListAutoPagingAsync(options).WithCancellation(cancellationToken))
            {
                if (transaction == null ||
                    string.IsNullOrWhiteSpace(transaction.Id) ||
                    transaction.AvailableOn == default ||
                    !string.Equals(transaction.Status, "available", StringComparison.OrdinalIgnoreCase) ||
                    !IsPaymentTransaction(transaction.Type, transaction.ReportingCategory))
                {
                    continue;
                }

                var paymentIntentId = string.Empty;
                var userId = string.Empty;
                var description = transaction.Description;
                var receiptUrl = string.Empty;
                Charge charge = null;
                if (!string.IsNullOrWhiteSpace(transaction.SourceId))
                {
                    try
                    {
                        if (transaction.SourceId.StartsWith("ch_", StringComparison.Ordinal))
                        {
                            charge = await new ChargeService().GetAsync(transaction.SourceId, cancellationToken: cancellationToken);
                            paymentIntentId = charge?.PaymentIntentId ?? string.Empty;
                            description = charge?.Description ?? description;
                            receiptUrl = charge?.ReceiptUrl ?? string.Empty;
                        }
                        else if (transaction.SourceId.StartsWith("pi_", StringComparison.Ordinal))
                        {
                            paymentIntentId = transaction.SourceId;
                        }

                        if (!string.IsNullOrWhiteSpace(paymentIntentId))
                        {
                            var paymentIntent = await new PaymentIntentService().GetAsync(
                                paymentIntentId,
                                cancellationToken: cancellationToken);
                            userId = MetadataValue(paymentIntent?.Metadata, "GamMaUserId") ?? string.Empty;
                            description = MetadataValue(paymentIntent?.Metadata, "ProductName")
                                ?? paymentIntent?.Description
                                ?? description;
                            if (string.IsNullOrWhiteSpace(userId))
                            {
                                var recurringMetadata = await ResolveRecurringPaymentMetadataAsync(
                                    paymentIntentId,
                                    cancellationToken);
                                userId = recurringMetadata.UserId ?? string.Empty;
                                description = recurringMetadata.ProductName ?? description;
                            }
                            if (string.IsNullOrWhiteSpace(receiptUrl) && !string.IsNullOrWhiteSpace(paymentIntent?.LatestChargeId))
                            {
                                charge = await new ChargeService().GetAsync(
                                    paymentIntent.LatestChargeId,
                                    cancellationToken: cancellationToken);
                                receiptUrl = charge?.ReceiptUrl ?? string.Empty;
                            }
                        }
                    }
                    catch (StripeException)
                    {
                        // The balance transaction remains useful even if its source
                        // object cannot be expanded with the current API key.
                    }
                }

                if (IsFullyRefundedBeforePayout(
                    charge,
                    payout.Created))
                {
                    continue;
                }

                result.Add(new StripeBalanceTransaction(
                    transaction.Id,
                    payoutId,
                    transaction.Created,
                    transaction.AvailableOn,
                    payout?.ArrivalDate,
                    ToMajorAmount(transaction.Amount),
                    ToMajorAmount(transaction.Fee),
                    ToMajorAmount(transaction.Net),
                    transaction.Currency,
                    transaction.Type,
                    transaction.ReportingCategory,
                    transaction.SourceId,
                    paymentIntentId,
                    description,
                    userId,
                    string.Empty,
                    receiptUrl));
            }

            return result;
        }

        public async Task<IReadOnlyList<StripeBalanceTransaction>> GetAvailablePaymentBalanceTransactionsAsync(
            DateTime createdFrom,
            DateTime createdToExclusive,
            CancellationToken cancellationToken = default)
        {
            var balanceTransactions = new BalanceTransactionService();
            var transactions = new List<BalanceTransaction>();
            var transactionOptions = new BalanceTransactionListOptions
            {
                Limit = 100,
                Created = new DateRangeOptions
                {
                    GreaterThanOrEqual = createdFrom,
                    LessThan = createdToExclusive
                }
            };

            await foreach (var transaction in balanceTransactions.ListAutoPagingAsync(transactionOptions).WithCancellation(cancellationToken))
            {
                if (transaction != null &&
                    !string.IsNullOrWhiteSpace(transaction.Id) &&
                    transaction.AvailableOn != default &&
                    string.Equals(transaction.Status, "available", StringComparison.OrdinalIgnoreCase) &&
                    IsPaymentTransaction(transaction.Type, transaction.ReportingCategory))
                {
                    transactions.Add(transaction);
                }
            }

            if (transactions.Count == 0)
            {
                return Array.Empty<StripeBalanceTransaction>();
            }

            // A balance transaction does not carry its payout ID when it is
            // listed globally. Build the mapping from the payout-specific
            // balance transaction endpoint where Stripe does expose it.
            var transactionIds = transactions
                .Select(transaction => transaction.Id)
                .ToHashSet(StringComparer.Ordinal);
            var payoutByTransactionId = new Dictionary<string, Payout>(StringComparer.Ordinal);
            var payoutService = new PayoutService();
            var payoutOptions = new PayoutListOptions
            {
                Limit = 100,
                Created = new DateRangeOptions
                {
                    GreaterThanOrEqual = createdFrom,
                    LessThan = createdToExclusive
                }
            };

            await foreach (var payout in payoutService.ListAutoPagingAsync(payoutOptions).WithCancellation(cancellationToken))
            {
                if (payout == null || string.IsNullOrWhiteSpace(payout.Id))
                {
                    continue;
                }

                var payoutTransactions = new BalanceTransactionListOptions
                {
                    Payout = payout.Id,
                    Limit = 100
                };
                await foreach (var payoutTransaction in balanceTransactions.ListAutoPagingAsync(payoutTransactions).WithCancellation(cancellationToken))
                {
                    if (payoutTransaction != null && transactionIds.Contains(payoutTransaction.Id))
                    {
                        payoutByTransactionId[payoutTransaction.Id] = payout;
                    }
                }
            }

            var result = new List<StripeBalanceTransaction>(transactions.Count);
            foreach (var transaction in transactions)
            {
                payoutByTransactionId.TryGetValue(transaction.Id, out var payout);
                var mapped = await MapBalanceTransactionAsync(
                    transaction,
                    payout?.Id,
                    payout?.Created,
                    transaction.AvailableOn,
                    cancellationToken);
                if (mapped != null)
                {
                    result.Add(mapped);
                }
            }

            return result;
        }

        private static async Task<StripeBalanceTransaction> MapBalanceTransactionAsync(
            BalanceTransaction transaction,
            string payoutId,
            DateTime? payoutCreated,
            DateTime payoutDate,
            CancellationToken cancellationToken)
        {
            var paymentIntentId = string.Empty;
            var userId = string.Empty;
            var description = transaction.Description;
            var receiptUrl = string.Empty;
            Charge charge = null;

            if (!string.IsNullOrWhiteSpace(transaction.SourceId))
            {
                try
                {
                    if (transaction.SourceId.StartsWith("ch_", StringComparison.Ordinal))
                    {
                        charge = await new ChargeService().GetAsync(transaction.SourceId, cancellationToken: cancellationToken);
                        paymentIntentId = charge?.PaymentIntentId ?? string.Empty;
                        description = charge?.Description ?? description;
                        receiptUrl = charge?.ReceiptUrl ?? string.Empty;
                    }
                    else if (transaction.SourceId.StartsWith("pi_", StringComparison.Ordinal))
                    {
                        paymentIntentId = transaction.SourceId;
                    }

                    if (!string.IsNullOrWhiteSpace(paymentIntentId))
                    {
                        var paymentIntent = await new PaymentIntentService().GetAsync(
                            paymentIntentId,
                            cancellationToken: cancellationToken);
                        userId = MetadataValue(paymentIntent?.Metadata, "GamMaUserId") ?? string.Empty;
                        description = MetadataValue(paymentIntent?.Metadata, "ProductName")
                            ?? paymentIntent?.Description
                            ?? description;
                        if (string.IsNullOrWhiteSpace(userId))
                        {
                            var recurringMetadata = await ResolveRecurringPaymentMetadataAsync(
                                paymentIntentId,
                                cancellationToken);
                            userId = recurringMetadata.UserId ?? string.Empty;
                            description = recurringMetadata.ProductName ?? description;
                        }
                        if (string.IsNullOrWhiteSpace(receiptUrl) && !string.IsNullOrWhiteSpace(paymentIntent?.LatestChargeId))
                        {
                            charge = await new ChargeService().GetAsync(
                                paymentIntent.LatestChargeId,
                                cancellationToken: cancellationToken);
                            receiptUrl = charge?.ReceiptUrl ?? string.Empty;
                        }
                    }
                }
                catch (StripeException)
                {
                    // The balance transaction remains useful even if its source
                    // object cannot be expanded with the current API key.
                }
            }

            if (payoutCreated.HasValue && IsFullyRefundedBeforePayout(charge, payoutCreated.Value))
            {
                return null;
            }

            return new StripeBalanceTransaction(
                transaction.Id,
                payoutId,
                transaction.Created,
                transaction.AvailableOn,
                payoutDate,
                ToMajorAmount(transaction.Amount),
                ToMajorAmount(transaction.Fee),
                ToMajorAmount(transaction.Net),
                transaction.Currency,
                transaction.Type,
                transaction.ReportingCategory,
                transaction.SourceId,
                paymentIntentId,
                description,
                userId,
                string.Empty,
                receiptUrl);
        }

        private sealed record RecurringPaymentMetadata(string UserId, string ProductName);

        private static async Task<RecurringPaymentMetadata> ResolveRecurringPaymentMetadataAsync(
            string paymentIntentId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(paymentIntentId))
            {
                return new RecurringPaymentMetadata(null, null);
            }

            var invoicePayments = await new InvoicePaymentService().ListAsync(
                new InvoicePaymentListOptions
                {
                    Payment = new InvoicePaymentPaymentOptions
                    {
                        PaymentIntent = paymentIntentId
                    },
                    Limit = 10
                },
                cancellationToken: cancellationToken);
            var invoicePayment = invoicePayments?.Data?.FirstOrDefault(payment =>
                !string.IsNullOrWhiteSpace(payment?.InvoiceId));
            if (invoicePayment == null)
            {
                return new RecurringPaymentMetadata(null, null);
            }

            var invoice = await new InvoiceService().GetAsync(
                invoicePayment.InvoiceId,
                cancellationToken: cancellationToken);
            var subscriptionDetails = invoice?.Parent?.SubscriptionDetails;
            var subscription = subscriptionDetails?.Subscription;
            if (subscription == null && !string.IsNullOrWhiteSpace(subscriptionDetails?.SubscriptionId))
            {
                subscription = await new SubscriptionService().GetAsync(
                    subscriptionDetails.SubscriptionId,
                    cancellationToken: cancellationToken);
            }

            return new RecurringPaymentMetadata(
                MetadataValue(subscription?.Metadata, "GamMaUserId")
                    ?? MetadataValue(subscriptionDetails?.Metadata, "GamMaUserId"),
                MetadataValue(subscription?.Metadata, "ProductName")
                    ?? MetadataValue(subscriptionDetails?.Metadata, "ProductName"));
        }

        private static bool IsFullyRefundedBeforePayout(
            Charge charge,
            DateTime payoutCreated)
        {
            if (charge == null || charge.Amount <= 0 || charge.Refunds?.Data == null)
            {
                return false;
            }

            var refundedAmount = charge.Refunds.Data
                .Where(refund =>
                    refund != null &&
                    string.Equals(refund.Status, "succeeded", StringComparison.OrdinalIgnoreCase) &&
                    refund.Created != default &&
                    refund.Created <= payoutCreated)
                .Sum(refund => refund.Amount);

            return refundedAmount >= charge.Amount;
        }

        public async Task<Product> GetProductAsync(string id)
        {
            try
            {
                return await new ProductService().GetAsync(id);
            }
            catch (StripeException)
            {
                return null;
            }
        }

        public async Task<Product[]> GetAllProductsAsync()
        {
            var options = new ProductListOptions
            {
                Limit = 100,
                Active = true
            };
            var service = new ProductService();
            return (await service.ListAsync(options)).ToArray();
        }

        public async Task<Product> GetProductByExactNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            return (await GetAllProductsAsync())
                .FirstOrDefault(product => string.Equals(product.Name, name.Trim(), StringComparison.Ordinal));
        }

        public async Task<Product> CreateProductAsync(string name, string description)
        {
            try
            {
                return await new ProductService().CreateAsync(new ProductCreateOptions
                {
                    Name = name,
                    Description = description
                });
            }
            catch (StripeException)
            {
                return null;
            }
        }

        public Task<bool> DeactivateProductAsync(string id)
        {
            return SetProductActiveAsync(id, false);
        }

        public async Task<bool> SetProductActiveAsync(string id, bool active)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return true;
            }

            try
            {
                await new ProductService().UpdateAsync(id, new ProductUpdateOptions
                {
                    Active = active
                });
                return true;
            }
            catch (StripeException exception) when (string.Equals(exception.StripeError?.Code, "resource_missing", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            catch (StripeException)
            {
                return false;
            }
        }

        public async Task<Price> CreateOneTimePriceAsync(
            string productId,
            long amountMinor,
            string currency,
            string nickname = null)
        {
            try
            {
                return await new PriceService().CreateAsync(new PriceCreateOptions
                {
                    Product = productId,
                    UnitAmount = amountMinor,
                    Currency = currency,
                    Nickname = nickname
                });
            }
            catch (StripeException)
            {
                return null;
            }
        }

        public async Task<Price> CreateRecurringPriceAsync(
            string productId,
            long amountMinor,
            string currency,
            string interval,
            int intervalCount,
            string nickname = null)
        {
            try
            {
                return await new PriceService().CreateAsync(new PriceCreateOptions
                {
                    Product = productId,
                    UnitAmount = amountMinor,
                    Currency = currency,
                    Nickname = nickname,
                    Recurring = new PriceRecurringOptions
                    {
                        Interval = interval,
                        IntervalCount = intervalCount
                    }
                });
            }
            catch (StripeException)
            {
                return null;
            }
        }

        public async Task<Price> UpdatePriceNicknameAsync(string id, string nickname)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            try
            {
                return await new PriceService().UpdateAsync(id, new PriceUpdateOptions
                {
                    Nickname = nickname
                });
            }
            catch (StripeException)
            {
                return null;
            }
        }

        public async Task<bool> DeactivatePriceAsync(string id)
        {
            return await SetPriceActiveAsync(id, false);
        }

        public async Task<bool> SetPriceActiveAsync(string id, bool active)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return true;
            }

            try
            {
                await new PriceService().UpdateAsync(id, new PriceUpdateOptions
                {
                    Active = active
                });
                return true;
            }
            catch (StripeException exception) when (string.Equals(exception.StripeError?.Code, "resource_missing", StringComparison.OrdinalIgnoreCase))
            {
                // The Stripe price is already gone, so there is nothing left to deactivate.
                return true;
            }
            catch (StripeException)
            {
                return false;
            }
        }

        public async Task<Price> GetPriceAsync(string product)
        {
            var options = new PriceListOptions
            {
                Limit = 100,
                Active = true,
                Product = product
            };
            var service = new PriceService();
            try
            {
                var prices = (await service.ListAsync(options)).OrderByDescending(p => p.UnitAmount);
                return prices.FirstOrDefault();
            }
            catch (StripeException)
            {
                return null;
            }
        }

        public async Task<Price> GetPriceByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            try
            {
                return await new PriceService().GetAsync(id);
            }
            catch (StripeException)
            {
                return null;
            }
        }

        public async Task<Subscription> GetSubscriptionAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            try
            {
                return await new SubscriptionService().GetAsync(id);
            }
            catch (StripeException)
            {
                return null;
            }
        }

        public async Task<IReadOnlyList<StripeSubscriptionPayment>> GetSubscriptionPaymentsAsync(string subscriptionId)
        {
            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                return Array.Empty<StripeSubscriptionPayment>();
            }

            var invoices = await new InvoiceService().ListAsync(new InvoiceListOptions
            {
                Subscription = subscriptionId,
                Limit = 100
            });

            return invoices.Data
                .OrderByDescending(invoice => invoice.Created)
                .Select(invoice => new StripeSubscriptionPayment(
                    invoice.Id,
                    invoice.Status,
                    string.Equals(invoice.Status, "paid", StringComparison.OrdinalIgnoreCase)
                        ? invoice.AmountPaid
                        : invoice.AmountDue,
                    invoice.Currency,
                    invoice.Description ?? invoice.Number,
                    invoice.Created,
                    invoice.DueDate,
                    invoice.StatusTransitions?.PaidAt))
                .ToList();
        }

        public async Task<bool> UpdateSubscriptionPriceAsync(string subscriptionId, string priceId)
        {
            if (string.IsNullOrWhiteSpace(subscriptionId) || string.IsNullOrWhiteSpace(priceId))
            {
                return false;
            }

            try
            {
                var service = new SubscriptionService();
                var subscription = await service.GetAsync(subscriptionId);
                var item = subscription?.Items?.Data?.FirstOrDefault();
                if (item == null || string.IsNullOrWhiteSpace(item.Id))
                {
                    return false;
                }

                await service.UpdateAsync(subscriptionId, new SubscriptionUpdateOptions
                {
                    Items = new List<SubscriptionItemOptions>
                    {
                        new()
                        {
                            Id = item.Id,
                            Price = priceId
                        }
                    },
                    ProrationBehavior = "none"
                });

                return true;
            }
            catch (StripeException)
            {
                return false;
            }
        }

        public async Task<Subscription> CancelSubscriptionAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            try
            {
                return await new SubscriptionService().CancelAsync(id);
            }
            catch (StripeException)
            {
                return null;
            }
        }

        private Session GetCreateSession(SessionLineItemPriceDataOptions priceData, string user, string successUrl, string cancelUrl)
        {
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string>
                {
                    "card"
                },
                LineItems =
                [
                    new() {
                        PriceData = priceData,
                        Quantity = 1,
                    }
                ],
                Metadata = new Dictionary<string, string>
                {
                    { "Product", priceData?.Product },
                    { "ProductName", priceData?.ProductData?.Name }
                },
                ClientReferenceId = user,
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
            };
            var service = new SessionService();
            var session = service.Create(options);
            return session;
        }

        private Session GetCreateSession(string priceId, string productId, string productName, string user, string successUrl, string cancelUrl, string mode = "payment", IDictionary<string, string> metadata = null, string idempotencyKey = null)
        {
            var sessionMetadata = new Dictionary<string, string>
            {
                { "Product", productId },
                { "ProductName", productName }
            };
            if (metadata != null)
            {
                foreach (var entry in metadata)
                {
                    sessionMetadata[entry.Key] = entry.Value;
                }
            }

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string>
                {
                    "card"
                },
                LineItems =
                [
                    new SessionLineItemOptions
                    {
                        Price = priceId,
                        Quantity = 1
                    }
                ],
                Metadata = sessionMetadata,
                ClientReferenceId = user,
                Mode = mode,
                PaymentIntentData = string.Equals(mode, "payment", StringComparison.OrdinalIgnoreCase)
                    ? new SessionPaymentIntentDataOptions
                    {
                        Description = productName,
                        Metadata = new Dictionary<string, string>(sessionMetadata)
                    }
                    : null,
                SubscriptionData = string.Equals(mode, "subscription", StringComparison.OrdinalIgnoreCase)
                    ? new SessionSubscriptionDataOptions
                    {
                        Metadata = new Dictionary<string, string>
                        {
                            { "GamMaUserId", user },
                            { "Product", productId },
                            { "ProductName", productName }
                        }
                    }
                    : null,
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
            };

            if (string.Equals(mode, "subscription", StringComparison.OrdinalIgnoreCase) && metadata != null)
            {
                foreach (var entry in metadata)
                {
                    options.SubscriptionData.Metadata[entry.Key] = entry.Value;
                }
            }

            var requestOptions = string.IsNullOrWhiteSpace(idempotencyKey)
                ? null
                : new RequestOptions { IdempotencyKey = idempotencyKey };

            return new SessionService().Create(options, requestOptions);
        }

        private static decimal ToMajorAmount(long amountMinor) => amountMinor / 100m;

        private static string MetadataValue(IDictionary<string, string> metadata, string key) =>
            metadata != null && metadata.TryGetValue(key, out var value) ? value : null;

    }
}
