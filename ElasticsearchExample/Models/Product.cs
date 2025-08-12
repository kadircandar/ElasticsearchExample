using ElasticsearchExample.Interfaces;

namespace ElasticsearchExample.Models
{
    public class Product : IElasticDocument
    {
        public string Id { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool InStock { get; set; }
    }
}
