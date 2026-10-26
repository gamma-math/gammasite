using Stripe;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Linq;
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

    public class StripeService : IStripeService
    {
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

    }
}
