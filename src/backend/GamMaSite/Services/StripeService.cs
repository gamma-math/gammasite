using Stripe;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace GamMaSite.Services
{
    /*
     * Defines Stripe product, price, session, and checkout operations.
     */
    public interface IStripeService
    {
        string StartPayment(Product product, Price price, string user, string successUrl, string cancelUrl, string mode = "payment", IDictionary<string, string> metadata = null);

        bool IsPaymentComplete(Session session);

        Task<Session> GetSessionAsync(string sessionId);

        Task<Product[]> GetAllProductsAsync();

        Task<Product> GetProductAsync(string id);

        Task<Product> GetProductByExactNameAsync(string name);

        Task<Product> CreateProductAsync(string name, string description);

        Task<Price> CreateOneTimePriceAsync(string productId, long amountMinor, string currency);

        Task<Price> CreateRecurringPriceAsync(string productId, long amountMinor, string currency, string interval, int intervalCount);

        Task<bool> DeactivatePriceAsync(string id);

        Task<Price> GetPriceAsync(string product);

        Task<Price> GetPriceByIdAsync(string id);

        Task<Subscription> GetSubscriptionAsync(string id);

        Task<Subscription> CancelSubscriptionAsync(string id);
    }

    /*
     * Wraps Stripe SDK calls used by the PaymentOffers API and webhooks.
     */
    public class StripeService : IStripeService
    {
        public string StartPayment(Product product, Price price, string user, string successUrl, string cancelUrl, string mode = "payment", IDictionary<string, string> metadata = null)
        {
            Session session = GetCreateSession(price.Id, product.Id, product.Name, user, successUrl, cancelUrl, mode, metadata);

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

        public async Task<Price> CreateOneTimePriceAsync(string productId, long amountMinor, string currency)
        {
            try
            {
                return await new PriceService().CreateAsync(new PriceCreateOptions
                {
                    Product = productId,
                    UnitAmount = amountMinor,
                    Currency = currency
                });
            }
            catch (StripeException)
            {
                return null;
            }
        }

        public async Task<Price> CreateRecurringPriceAsync(string productId, long amountMinor, string currency, string interval, int intervalCount)
        {
            try
            {
                return await new PriceService().CreateAsync(new PriceCreateOptions
                {
                    Product = productId,
                    UnitAmount = amountMinor,
                    Currency = currency,
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

        public async Task<bool> DeactivatePriceAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return true;
            }

            try
            {
                await new PriceService().UpdateAsync(id, new PriceUpdateOptions
                {
                    Active = false
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
                    { "SessionCreated", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture) },
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

        private Session GetCreateSession(string priceId, string productId, string productName, string user, string successUrl, string cancelUrl, string mode = "payment", IDictionary<string, string> metadata = null)
        {
            var sessionMetadata = new Dictionary<string, string>
            {
                { "SessionCreated", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture) },
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

            return new SessionService().Create(options);
        }

    }
}
