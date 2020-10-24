using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GamMaSite.Services
{
    public interface IStripeService
    {
        string StartPayment(string id, long price, string currency, string productName, string mode, string successUrl, string cancelUrl);
    }

    public class StripeService : IStripeService
    {
        public string StartPayment(string id, long price, string currency, string productName, string mode, string successUrl, string cancelUrl)
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
                        PriceData = id != null ? GetPriceData(id) :  GetPriceData(price, currency, productName),
                        Quantity = 1,
                    },
                },
                Mode = mode,
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
            };
            var service = new SessionService();
            Session session = service.Create(options);

            return session.Id;
        }

        private SessionLineItemPriceDataOptions GetPriceData(long price, string currency, string productName)
        {
            return new SessionLineItemPriceDataOptions
            {
                UnitAmount = price,
                Currency = currency,
                ProductData = new SessionLineItemPriceDataProductDataOptions
                {
                    Name = productName,
                },
            };
        }

        private SessionLineItemPriceDataOptions GetPriceData(string id)
        {
            return new SessionLineItemPriceDataOptions
            {
                Product = id
            };
        }
    }
}
