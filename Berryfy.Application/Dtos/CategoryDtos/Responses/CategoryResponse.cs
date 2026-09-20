using Berryfy.Application.Dtos.ProductDtos.Responses;

namespace Berryfy.Application.Dtos.CategoryDtos.Responses
{
    public class CategoryResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
    }
}
