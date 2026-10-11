using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.CategoryDtos.Requests;
using Berryfy.Application.Dtos.CategoryDtos.Responses;
using Berryfy.Domain.Entities;
using Berryfy.Domain.Entities.ProductEntities;

namespace Berryfy.Application.Services.Interfaces.ProductServiceInterfaces
{
    public interface ICategoryService
    {
        Task<ApplicationResponse<IEnumerable<GlobalCategoryResponse>>> GetAllAsync();
        Task<ApplicationResponse<GlobalCategoryResponse>> GetByIdAsync(int id);
        Task<ApplicationResponse<GlobalCategoryResponse>> GetByNameAsync(string name);
        Task<ApplicationResponse<GlobalCategoryResponse>> CreateAsync(CreateCategoryRequest categoryDto);
        Task<ApplicationResponse<GlobalCategoryResponse>> UpdateAsync(int id, UpdateCategoryRequest categoryDto);
        Task<ApplicationResponse<bool>> DeleteAsync(int id);
        Task<ApplicationResponse<bool>> ExistsAsync(int id);
        Task<ApplicationResponse<bool>> ExistsByNameAsync(string name);
        Task<ApplicationResponse<List<Product>>> GetProductsByCategoryId(int categoryId);
        Task<InfrastructureResponse<int>> GetProductCountByCategoryIdAsync(int categoryId);
    }
}
