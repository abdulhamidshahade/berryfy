using Berryfy.Domain.Entities.ShopEntities;

namespace Berryfy.Application.Dtos.CategoryDtos.Responses
{
    public class ShopCategoryResponse
    {
        public int Id { get; set; }
        public int ShopId { get; set; }
        public int? GlobalCategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string Slug { get; set; } = string.Empty;
        public int? ParentCategoryId { get; set; }
        public bool IsActive { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        //public static ShopCategoryResponse MapFromShopCategory(ShopCategory category)
        //{
        //    return new ShopCategoryResponse
        //    {
        //        Id = category.Id,
        //        ShopId = category.ShopId,
        //        Name = category.Name,
        //        Description = category.Description,
        //        ImageUrl = category.ImageUrl,
        //        Slug = category.Slug,
        //        ParentCategoryId = category.ParentCategoryId,
        //        IsActive = category.IsActive,
        //        DisplayOrder = category.DisplayOrder,
        //        CreatedAt = category.CreatedAt,
        //        UpdatedAt = category.UpdatedAt,
        //        GlobalCategoryId = category.GlobalCategoryId
        //    };
        //}
    }
}
