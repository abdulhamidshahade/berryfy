using Berryfy.Application.Dtos.CategoryDtos.Requests;
using Berryfy.Application.Dtos.CategoryDtos.Responses;

namespace Berryfy.Application.Services.Interfaces.ProductServiceInterfaces
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryResponse>> GetAllAsync();
        Task<CategoryResponse> GetByIdAsync(int id);
        Task<CategoryResponse> GetByNameAsync(string name);
        Task<CategoryResponse> CreateAsync(CreateCategoryRequest categoryDto);
        Task<CategoryResponse> UpdateAsync(int id, UpdateCategoryRequest categoryDto);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<bool> ExistsByNameAsync(string name);
    }
}
