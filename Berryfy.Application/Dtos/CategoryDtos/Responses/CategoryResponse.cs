using Berryfy.Application.Dtos.ProductDtos.Responses;
using Berryfy.Domain.Entities.ProductEntities;

namespace Berryfy.Application.Dtos.CategoryDtos.Responses
{
    public class CategoryResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }

        public static Category MapToCategory(CategoryResponse response)
        {
            return new Category
            {
                Id = response.Id,
                Name = response.Name,
                Description = response.Description,
                ImageUrl = response.ImageUrl
            };
        }

        public static CategoryResponse MapFromCategory(Category category)
        {
            return new CategoryResponse
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                ImageUrl = category.ImageUrl
            };
        }
    }
}
