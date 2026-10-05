using Berryfy.Domain.Entities;
using Berryfy.Domain.Entities.ProductEntities;

namespace Berryfy.Domain.Repositories.ProductInterfaces
{
    public interface IProductRepository
    {
        Task<InfrastructureResponse<IReadOnlyList<Product>>> GetAllAsync();
        Task<InfrastructureResponse<Product>> GetByIdAsync(int id);
        Task<InfrastructureResponse<Product>> GetByNameAsync(string name);
        Task<InfrastructureResponse<Product>> CreateAsync(Product product);
        Task<InfrastructureResponse<Product>> UpdateAsync(int id, Product product);
        Task<InfrastructureResponse<bool>> DeleteAsync(Product product);
        Task<InfrastructureResponse<bool>> ExistsByIdAsync(int id);
        Task<InfrastructureResponse<bool>> ExistsByNameAsync(string name);
        Task<InfrastructureResponse<int>> GetTotalCountAsync();

        Task<InfrastructureResponse<IReadOnlyList<Product>>> GetFilteredAsync(string? searchTerm = null, string? category = null,
            string? sortBy = "name", decimal? minPrice = null, decimal? maxPrice = null,
            bool? isActive = true, int pageNumber = 1, int pageSize = 10);

        Task<InfrastructureResponse<int>> GetFilteredCountAsync(string? searchTerm = null, string? category = null,
            decimal? minPrice = null, decimal? maxPrice = null, bool? isActive = true);
    }
}
