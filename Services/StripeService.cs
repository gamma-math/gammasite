using Stripe;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GamMaSite.Services
{
    public interface IStripeService
    {
        string StartPayment(string product, long price, string currency, string user, string successUrl, string cancelUrl);

        string StartPayment(string name, string description, long price, string currency, string user, string successUrl, string cancelUrl);

        bool IsPaymentComplete(Session session);

        Task<Session> GetSessionAsync(string sessionId);

        Task<Product[]> GetAllProductsAsync();

        Task<Product> GetProductAsync(string id);

        Task<Product> GetProductByNameAsync(string name);

        Task<Price> GetPriceAsync(string id);
    }

    public class StripeService : IStripeService
    {
        public string StartPayment(string product, long price, string currency, string user, string successUrl, string cancelUrl)
        {
            Session session = GetCreateSession(GetPriceData(product, price, currency), user, successUrl, cancelUrl);

            return session.Id;
        }

        public string StartPayment(string name, string description, long price, string currency, string user, string successUrl, string cancelUrl)
        {
            Session session = GetCreateSession(GetPriceData(name, description, price, currency), user, successUrl, cancelUrl);

            return session.Id;
        }

        public bool IsPaymentComplete(Session session)
        {
            var paymentComplete = new string[] { "paid", "no_payment_required" }.Contains(session?.PaymentStatus);
            return paymentComplete;
        }

        public async Task<Session> GetSessionAsync(string sessionId)
        {
            try
            {
                var session = await new SessionService().GetAsync(sessionId);
                return session;
            }
            catch (StripeException ex)
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
            var products = (await service.ListAsync(options)).ToArray();
            return products;
        }

        public async Task<Product> GetProductAsync(string id)
        {
            return await new ProductService().GetAsync(id);
        }

        public async Task<Product> GetProductByNameAsync(string name)
        {
            var matches = (await GetAllProductsAsync()).Where(p => p.Name.ToLower().Contains(name.ToLower()));
            return matches.OrderBy(p => p.Name.Length).FirstOrDefault();
        }

        public async Task<Price> GetPriceAsync(string productID)
        {
            var options = new PriceListOptions
            {
                Limit = 100,
                Active = true,
                Product = productID
            };
            var service = new PriceService();
            var prices = (await service.ListAsync(options)).OrderByDescending(p => p.UnitAmount);
            return prices.FirstOrDefault();
        }

        private Session GetCreateSession(SessionLineItemPriceDataOptions priceData, string user, string successUrl, string cancelUrl)
        {
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string>
                {
                    "card"
                },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = priceData,
                        Quantity = 1,
                    }
                },
                Metadata = new Dictionary<string, string> 
                { 
                    { "SessionCreated", DateTime.Now.ToString() },
                    { "Product", priceData.Product },
                    { "ProductName", priceData.ProductData.Name }
                },
                ClientReferenceId = user,
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
            };
            var service = new SessionService();
            Session session = service.Create(options);
            return session;
        }

        private SessionLineItemPriceDataOptions GetPriceData(string product, long price, string currency)
        {
            return new SessionLineItemPriceDataOptions
            {
                Product = product,
                UnitAmount = price,
                Currency = currency
            };
        }

        private SessionLineItemPriceDataOptions GetPriceData(string name, string description, long price, string currency)
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
