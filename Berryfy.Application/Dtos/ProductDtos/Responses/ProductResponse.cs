using Berryfy.Application.Dtos.CategoryDtos.Responses;
using Berryfy.Domain.Entities.ProductEntities;

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
        public int CategoryId { get; set; }
        public int ShopId { get; set; }
        public int? ShopCategoryId { get; set; }
        public List<CategoryResponse> ProductCategories { get; set; } = new List<CategoryResponse>();

        public static ProductResponse MapFromProduct(Product product)
        {
            if (product == null)
            {
                return null!;
            }

            return new ProductResponse
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                ImageUrl = product.ImageUrl,
                ReservedStock = product.ReservedStock,
                LowStockThreshold = product.LowStockThreshold,
                IsActive = product.IsActive,
                SKU = product.SKU,
                CategoryId = product.CategoryId
            };
        }

        public static Product MapToProduct(ProductResponse response)
        {
            if (response == null)
            {
                return null!;
            }

            return new Product
            {
                Id = response.Id,
                Name = response.Name,
                Description = response.Description,
                Price = response.Price,
                StockQuantity = response.StockQuantity,
                ImageUrl = response.ImageUrl,
                ReservedStock = response.ReservedStock,
                LowStockThreshold = response.LowStockThreshold,
                IsActive = response.IsActive,
                SKU = response.SKU,
                CategoryId = response.CategoryId
            };
        }

        public static List<ProductResponse> MapFromProduct(IEnumerable<Product> products)
        {
            List<ProductResponse> productResponses = new List<ProductResponse>();

            foreach(Product product in products)
            {
                productResponses.Add(MapFromProduct(product));
            }

            return productResponses;
        }
    }
}