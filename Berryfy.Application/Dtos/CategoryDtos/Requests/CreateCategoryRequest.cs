using Berryfy.Domain.Entities.ProductEntities;

namespace Berryfy.Application.Dtos.CategoryDtos.Requests
{
    public class CreateCategoryRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }

        public static Category MapToCategory(CreateCategoryRequest request)
        {
            return new Category
            {
                Name = request.Name,
                Description = request.Description,
                ImageUrl = request.ImageUrl,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
