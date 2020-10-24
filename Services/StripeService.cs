using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GamMaSite.Services
{
    public interface IStripeService
    {
        string StartPayment(long price, string currency, string productName, string mode, string successUrl, string cancelUrl);
    }

    public class StripeService : IStripeService
    {
        public string StartPayment(long price, string currency, string productName, string mode, string successUrl, string cancelUrl)
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
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = price,
                            Currency = currency,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = productName,
                            },
                        },
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
    }
}
