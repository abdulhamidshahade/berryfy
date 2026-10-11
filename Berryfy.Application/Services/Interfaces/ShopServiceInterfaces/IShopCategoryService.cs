using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.CategoryDtos.Requests;
using Berryfy.Application.Dtos.CategoryDtos.Responses;
using Berryfy.Application.Dtos.ProductDtos.Responses;

namespace Berryfy.Application.Services.Interfaces.ShopServiceInterfaces
{
    public interface IShopCategoryService
    {
        Task<ApplicationResponse<IEnumerable<ShopCategoryResponse>>> GetAllAsync(int shopId, bool includeInactive = false);
        Task<ApplicationResponse<ShopCategoryResponse>> GetByIdAsync(int shopId, int id);
        Task<ApplicationResponse<ShopCategoryResponse>> GetBySlugAsync(int shopId, string slug);
        Task<ApplicationResponse<ShopCategoryResponse>> CreateAsync(int shopId, CreateShopCategoryRequest request);
        Task<ApplicationResponse<ShopCategoryResponse>> UpdateAsync(int shopId, int id, UpdateShopCategoryRequest request);
        Task<ApplicationResponse<bool>> DeleteAsync(int shopId, int id);
        Task<ApplicationResponse<bool>> ExistsAsync(int shopId, int id);
        Task<ApplicationResponse<List<ProductResponse>>> GetProductsAsync(int shopId, int id);
        Task<ApplicationResponse<int>> GetProductCountAsync(int shopId, int id);
    }
}