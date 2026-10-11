using Berryfy.Domain.Entities.ProductEntities;

namespace Berryfy.Application.Dtos.CategoryDtos.Requests
{
    public class UpdateCategoryRequest
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string Slug { get; set; }
        public int? ParentCategoryId { get; set; }
        public bool IsActive { get; set; }
        public int DisplayOrder { get; set; }

        public static GlobalCategory MapToCategory(UpdateCategoryRequest request)
        {
            return new GlobalCategory
            {
                Id = request.Id,
                Name = request.Name,
                Description = request.Description,
                ImageUrl = request.ImageUrl,
                Slug = request.Slug,
                ParentCategoryId = request.ParentCategoryId,
                IsActive = request.IsActive,
                DisplayOrder = request.DisplayOrder,
                UpdatedAt = DateTime.Now,
            };
        }
    }
}
