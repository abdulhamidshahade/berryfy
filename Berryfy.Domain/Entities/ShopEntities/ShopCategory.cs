using Berryfy.Domain.Entities.Base;

namespace Berryfy.Domain.Entities.ShopEntities
{
    public class ShopCategory : IAuditableEntity
    {
        public int Id { get; set; }
        public int ShopId { get; set; }
        public int? GlobalCategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string Slug { get; set; } = string.Empty;
        public int? ParentCategoryId { get; set; }

        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
