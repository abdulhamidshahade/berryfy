using Berryfy.Application.Dtos.CategoryDtos.Responses;

namespace Berryfy.Application.Dtos.ProductDtos.Responses
{
    public class ProductResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string ImageUrl { get; set; }

        public int ReservedStock { get; set; }
        public int LowStockThreshold { get; set; } = 10;

        public bool IsActive { get; set; } = true;

        public string SKU { get; set; }
        public List<CategoryResponse> ProductCategories { get; set; }
    }
}
