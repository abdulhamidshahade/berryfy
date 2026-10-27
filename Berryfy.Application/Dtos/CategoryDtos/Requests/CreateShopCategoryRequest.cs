using Berryfy.Domain.Entities.ShopEntities;

namespace Berryfy.Application.Dtos.CategoryDtos.Requests
{
    public class CreateShopCategoryRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }

        public string? Slug { get; set; }

        public int? ParentCategoryId { get; set; }
        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; }

        //public static ShopCategory MapToShopCategory(int shopId, CreateShopCategoryRequest request, string slug)
        //{
        //    return new ShopCategory
        //    {
        //        ShopId = shopId,
        //        Name = request.Name.Trim(),
        //        Description = request.Description,
        //        ImageUrl = request.ImageUrl,
        //        Slug = slug,
        //        ParentCategoryId = request.ParentCategoryId,
        //        IsActive = request.IsActive,
        //        DisplayOrder = request.DisplayOrder
        //    };
        //}
    }
}
