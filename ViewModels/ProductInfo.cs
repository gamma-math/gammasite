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

        public Price[] Prices { get; set; }

        public ProductInfo(Product product, Price[] prices)
        {
            this.Product = product;
            this.Prices = prices;
        }
    }
}
