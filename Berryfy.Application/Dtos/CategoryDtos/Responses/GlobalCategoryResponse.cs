using Berryfy.Application.Dtos.ProductDtos.Responses;
using Berryfy.Domain.Entities.ProductEntities;

namespace Berryfy.Application.Dtos.CategoryDtos.Responses
{
    public class GlobalCategoryResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string Slug { get; set; }
        public int? ParentCategoryId { get; set; }
        public bool IsActive { get; set; }
        public int DisplayOrder { get; set; }

        public static GlobalCategory MapToCategory(GlobalCategoryResponse response)
        {
            return new GlobalCategory
            {
                Id = response.Id,
                Name = response.Name,
                Description = response.Description,
                ImageUrl = response.ImageUrl
            };
        }

        public static GlobalCategoryResponse MapFromCategory(GlobalCategory category)
        {
            return new GlobalCategoryResponse
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                ImageUrl = category.ImageUrl,
                Slug = category.Slug,
                ParentCategoryId= category.ParentCategoryId,
                IsActive = category.IsActive,
                DisplayOrder = category.DisplayOrder,
            };
        }
    }
}
