using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.ProductDtos.Requests;
using Berryfy.Application.Dtos.ProductDtos.Responses;

namespace Berryfy.Application.Services.Interfaces.ProductServiceInterfaces
{
    public interface IProductService
    {
        Task<IReadOnlyList<ProductResponse>> GetAllAsync();
        Task<PaginationDto<ProductResponse>> GetPaginatedAsync(ProductFilter filter);
        Task<ProductResponse> GetByIdAsync(int id);
        Task<ProductResponse> GetByNameAsync(string name);
        Task<ProductResponse> CreateAsync(CreateProduct productDto, List<int> categories);
        Task<ProductResponse> UpdateAsync(int id, UpdateProduct productDto, List<int> categories);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsByIdAsync(int id);
        Task<bool> ExistsByNameAsync(string name);
    }
}
