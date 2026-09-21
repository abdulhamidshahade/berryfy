using Berryfy.Domain.Entities.ProductEntities;

namespace Berryfy.Application.Dtos.ProductDtos.Requests
{
    public class UpdateProduct
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

        public static Product MapToProduct(UpdateProduct request)
        {
            return new Product
            {
                Id = request.Id,
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                StockQuantity = request.StockQuantity,
                ImageUrl = request.ImageUrl,
                ReservedStock = request.ReservedStock,
                LowStockThreshold = request.LowStockThreshold,
                IsActive = request.IsActive,
                SKU = request.SKU
            };
        }
    }
}
