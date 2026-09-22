using System.Text.Json.Serialization;
using Berryfy.Domain.Entities.Base;
using Berryfy.Domain.Entities.InventoryEntities;
using Berryfy.Domain.Entities.OrderEntities;
using Berryfy.Domain.Entities.ShoppingCartEntities;

namespace Berryfy.Domain.Entities.ProductEntities
{
    public class Product : IAuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int StockQuantity { get; set; }
        public string ImageUrl { get; set; }
        public decimal Price { get; set; }
        public int ReservedStock { get; set; }
        public int LowStockThreshold { get; set; } = 5;
        public bool IsActive { get; set; } = true;
        public string SKU { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        [JsonIgnore] public List<InventoryLog> InventoryLogs { get; set; }
        [JsonIgnore] public List<ProductCategory> ProductCategories { get; set; }
        [JsonIgnore] public List<CartItem> CartItems { get; set; }
        [JsonIgnore] public List<OrderItem> OrderItems { get; set; }
    }
}
