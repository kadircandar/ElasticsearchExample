using ElasticsearchExample.Models;
using System;

namespace ElasticsearchExample.TestData
{
    public static class TestDataGenerator
    {
        public static List<Product> GenerateProducts(int count)
        {
            var products = new List<Product>();
            var random = new Random();

            for (int i = 1; i <= count; i++)
            {
                products.Add(new Product
                {
                    Id = i.ToString(),
                    Description = $"Sample Product {i}",
                    Price = Math.Round((decimal)(random.NextDouble() * 1000), 2),
                    CreatedAt = DateTime.UtcNow.AddDays(-random.Next(0, 365)),
                    InStock = random.Next(0, 2) == 1
                });
            }

            return products;
        }

        public static Product GenerateProduct()
        {
            var random = new Random();
            return new Product
            {
                Id = "1",
                Description = $"Sample Product Create or Update",
                Price = Math.Round((decimal)(random.NextDouble() * 1000), 2),
                CreatedAt = DateTime.UtcNow.AddDays(-random.Next(0, 365)),
                InStock = random.Next(0, 2) == 1
            };
        }
    }
}
