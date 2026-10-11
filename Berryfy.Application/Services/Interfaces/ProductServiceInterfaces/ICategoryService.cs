using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.CategoryDtos.Requests;
using Berryfy.Application.Dtos.CategoryDtos.Responses;
using Berryfy.Domain.Entities;
using Berryfy.Domain.Entities.ProductEntities;

namespace Berryfy.Application.Services.Interfaces.ProductServiceInterfaces
{
    public interface ICategoryService
    {
        Task<ApplicationResponse<IEnumerable<CategoryResponse>>> GetAllAsync();
        Task<ApplicationResponse<CategoryResponse>> GetByIdAsync(int id);
        Task<ApplicationResponse<CategoryResponse>> GetByNameAsync(string name);
        Task<ApplicationResponse<CategoryResponse>> CreateAsync(CreateCategoryRequest categoryDto);
        Task<ApplicationResponse<CategoryResponse>> UpdateAsync(int id, UpdateCategoryRequest categoryDto);
        Task<ApplicationResponse<bool>> DeleteAsync(int id);
        Task<ApplicationResponse<bool>> ExistsAsync(int id);
        Task<ApplicationResponse<bool>> ExistsByNameAsync(string name);
        Task<ApplicationResponse<List<Product>>> GetProductsByCategoryId(int categoryId);
        Task<InfrastructureResponse<int>> GetProductCountByCategoryIdAsync(int categoryId);
    }
}
