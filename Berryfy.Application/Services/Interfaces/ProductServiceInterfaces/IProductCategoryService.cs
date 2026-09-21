using Berryfy.Application.Dtos.ProductDtos.Responses;

namespace Berryfy.Application.Services.Interfaces.ProductServiceInterfaces
{
    public interface IProductCategoryService
    {
        Task<bool> AddProductCategoryAsync(ProductResponse Product, List<int> categories);
        Task<bool> UpdateProductCategoryAsync(ProductResponse product, List<int> categories);
    }
}
