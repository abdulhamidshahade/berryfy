using Berryfy.Domain.Entities;
using Berryfy.Domain.Entities.ProductEntities;

namespace Berryfy.Domain.Repositories.ProductInterfaces
{
    public interface ICategoryRepository
    {
        Task<InfrastructureResponse<GlobalCategory>> GetByIdAsync(int id);
        Task<InfrastructureResponse<GlobalCategory>> GetByNameAsync(string name);
        Task<InfrastructureResponse<IEnumerable<GlobalCategory>>> GetAllAsync();
        Task<InfrastructureResponse<GlobalCategory>> CreateAsync(GlobalCategory category);
        Task<InfrastructureResponse<GlobalCategory>> UpdateAsync(int id, GlobalCategory category);
        Task<InfrastructureResponse<bool>> DeleteAsync(GlobalCategory category);
        Task<InfrastructureResponse<bool>> ExistsByIdAsync(int id);
        Task<InfrastructureResponse<bool>> ExistsByNameAsync(string name);
        Task<InfrastructureResponse<List<Product>>> GetProductsByGlobalCategoryId(int categoryId);
        Task<InfrastructureResponse<int>> GetProductCountByGlobalCategoryIdAsync(int categoryId);
    }
}
