using System.Text.Json.Serialization;
using Berryfy.Domain.Entities.Base;

namespace Berryfy.Domain.Entities.ProductEntities
{
    public class GlobalCategory : IAuditableEntity
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string Slug { get; set; }
        public int? ParentCategoryId { get; set; }
        public bool IsActive { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [JsonIgnore] public List<ProductCategory> ProductCategories { get; set; }
    }
}
