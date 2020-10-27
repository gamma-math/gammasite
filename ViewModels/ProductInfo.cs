using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GamMaSite.ViewModels
{
    public class ProductInfo
    { 
        public Product Product { get; set; }

        public Price Price { get; set; }

        public string ApiKey { get; set; }

        public ProductInfo(Product product, Price price, string apiKey)
        {
            this.Product = product;
            this.Price = price;
            this.ApiKey = apiKey;
        }
    }
}
