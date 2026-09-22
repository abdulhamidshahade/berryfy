using System.Text.Json.Serialization;
using Berryfy.Domain.Constants;
using Berryfy.Domain.Entities.Base;
using Berryfy.Domain.Entities.ProductEntities;

namespace Berryfy.Domain.Entities.InventoryEntities
{
    public class InventoryLog : IAuditableEntity
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        [JsonIgnore] public Product Product { get; set; }

        public int CurrentStockQuantity { get; set; }

        public int QuantityChanged { get; set; }

        public InventoryChangeType ChangeType { get; set; }

        public int ReferenceId { get; set; }
        public string ReferenceType { get; set; }

        public int? PerformedByUserId { get; set; }

        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
