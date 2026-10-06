using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.ProductDtos.Responses;

namespace Berryfy.Application.Services.Interfaces.ProductServiceInterfaces
{
    public interface IProductCategoryService
    {
        Task<ApplicationResponse<bool>> AddProductCategoryAsync(ProductResponse Product, List<int> categories);
        Task<ApplicationResponse<bool>> UpdateProductCategoryAsync(ProductResponse product, List<int> categories);
    }
}
