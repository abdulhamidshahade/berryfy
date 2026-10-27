using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.CategoryDtos.Responses;
using Berryfy.Application.Dtos.ProductDtos.Requests;
using Berryfy.Application.Dtos.ProductDtos.Responses;

namespace Berryfy.Application.Services.Interfaces.ProductServiceInterfaces
{
    public interface IProductService
    {
        Task<ApplicationResponse<IReadOnlyList<ProductResponse>>> GetAllAsync();
        Task<ApplicationResponse<PaginationResponse<ProductResponse>>> GetPaginatedAsync(ProductFilter filter);
        Task<ApplicationResponse<ProductResponse>> GetByIdAsync(int id);
        Task<ApplicationResponse<ProductResponse>> GetByNameAsync(string name);
        Task<ApplicationResponse<ProductResponse>> CreateAsync(CreateProduct productDto);
        Task<ApplicationResponse<ProductResponse>> UpdateAsync(int id, UpdateProduct productDto);
        Task<ApplicationResponse<bool>> DeleteAsync(int id);
        Task<ApplicationResponse<bool>> ExistsByIdAsync(int id);
        Task<ApplicationResponse<bool>> ExistsByNameAsync(string name);
        //Task<ApplicationResponse<ShopCategoryResponse>> GetShopCategoryByProductAsync(int productId);
    }
}
