using System.Text.Json.Serialization;
using Berryfy.Domain.Entities.Base;
using Berryfy.Domain.Entities.ProductEntities;

namespace Berryfy.Domain.Entities.ShoppingCartEntities
{
    public class CartItem : IAuditableEntity
    {
        public int Id { get; set; }
        public int? ShoppingCartId { get; set; }
        public int? UserId { get; set; }
        public string? SessionId { get; set; }
        [JsonIgnore] public Cart ShoppingCart { get; set; }
        public int ProductId { get; set; }  
        [JsonIgnore] public Product Product { get; set; }
        public int Quantity { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public decimal UnitPrice { get; set; }

    }
}
