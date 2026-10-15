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
        string StartPayment(Product product, Price price, string user, string successUrl, string cancelUrl, string mode = "payment");

        string StartPayment(string name, string description, long price, string currency, string user, string successUrl, string cancelUrl);

        bool IsPaymentComplete(Session session);

        Task<Session> GetSessionAsync(string sessionId);

        Task<Product[]> GetAllProductsAsync();

        Task<Product[]> GetProductsAsync(List<string> ids);

        Task<Product> GetProductAsync(string id);

        Task<Product> GetProductByNameAsync(string name);

        Task<Price> GetPriceAsync(string product);

        Task<Price> GetPriceByIdAsync(string id);

        Task<Subscription> GetSubscriptionAsync(string id);

        Task<Subscription> CancelSubscriptionAsync(string id);
    }

    /*
     * Wraps Stripe SDK calls used by payment APIs and legacy payment redirects.
     */
    public class StripeService : IStripeService
    {
        public string StartPayment(Product product, Price price, string user, string successUrl, string cancelUrl, string mode = "payment")
        {
            Session session = GetCreateSession(price.Id, product.Id, product.Name, user, successUrl, cancelUrl, mode);

            return session.Id;
        }

        public string StartPayment(string name, string description, long price, string currency, string user, string successUrl, string cancelUrl)
        {
            var session = GetCreateSession(GetPriceData(name, description, price, currency), user, successUrl, cancelUrl);

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

        public async Task<Product[]> GetAllProductsAsync()
        {
            var service = new ProductService();
            var products = (await service.ListAsync(GetProductListOptions())).ToArray();
            return products;
        }

        public async Task<Product[]> GetProductsAsync(List<string> ids)
        {
            var options = GetProductListOptions();
            options.Ids = ids;
            var service = new ProductService();
            var products = (await service.ListAsync(options)).ToArray();
            return products;
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

        public async Task<Product> GetProductByNameAsync(string name)
        {
            var matches = (await GetAllProductsAsync()).Where(p => p.Name.ToLower().Contains(name.ToLower()));
            return matches.MinBy(p => p.Name.Length);
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

        private Session GetCreateSession(string priceId, string productId, string productName, string user, string successUrl, string cancelUrl, string mode = "payment")
        {
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
                Metadata = new Dictionary<string, string>
                {
                    { "SessionCreated", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture) },
                    { "Product", productId },
                    { "ProductName", productName }
                },
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

            return new SessionService().Create(options);
        }

        private static ProductListOptions GetProductListOptions()
        {
            return new ProductListOptions
            {
                Limit = 100,
                Active = true
            };
        }

        private static SessionLineItemPriceDataOptions GetPriceData(string name, string description, long price, string currency)
        {
            return new SessionLineItemPriceDataOptions
            {
                UnitAmount = price,
                Currency = currency,
                ProductData = new SessionLineItemPriceDataProductDataOptions
                {
                    Description = description,
                    Name = name
                }
            };
        }
    }
}
