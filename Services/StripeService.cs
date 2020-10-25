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
        string StartPayment(string product, long price, string currency, string successUrl, string cancelUrl);

        string StartPayment(string name, string description, long price, string currency, string successUrl, string cancelUrl);

        Task<Product[]> GetAllProductsAsync();

        Task<Product> GetProductAsync(string id);

        Task<Price[]> GetPricesAsync(string productID);

        Task<Price> GetPriceAsync(string id);
    }

    public class StripeService : IStripeService
    {
        public string StartPayment(string product, long price, string currency, string successUrl, string cancelUrl)
        {
            Session session = GetCreateSession(GetPriceData(product, price, currency), successUrl, cancelUrl);

            return session.Id;
        }

        public string StartPayment(string name, string description, long price, string currency, string successUrl, string cancelUrl)
        {
            Session session = GetCreateSession(GetPriceData(name, description, price, currency), successUrl, cancelUrl);

            return session.Id;
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

        public async Task<Price[]> GetPricesAsync(string productID)
        {
            var options = new PriceListOptions
            {
                Limit = 100,
                Active = true,
                Product = productID
            };
            var service = new PriceService();
            var products = (await service.ListAsync(options)).ToArray();
            return products;
        }

        public async Task<Price> GetPriceAsync(string id)
        {
            var price = (await new PriceService().GetAsync(id));
            return price;
        }

        private Session GetCreateSession(SessionLineItemPriceDataOptions priceData, string successUrl, string cancelUrl)
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
