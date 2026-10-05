using Berryfy.Domain.Entities;
using Berryfy.Domain.Entities.ProductEntities;

namespace Berryfy.Domain.Repositories.ProductInterfaces
{
    public interface ICategoryRepository
    {
        Task<InfrastructureResponse<Category>> GetByIdAsync(int id);
        Task<InfrastructureResponse<Category>> GetByNameAsync(string name);
        Task<InfrastructureResponse<IEnumerable<Category>>> GetAllAsync();
        Task<InfrastructureResponse<Category>> CreateAsync(Category category);
        Task<InfrastructureResponse<Category>> UpdateAsync(int id, Category category);
        Task<InfrastructureResponse<bool>> DeleteAsync(Category category);
        Task<InfrastructureResponse<bool>> ExistsByIdAsync(int id);
        Task<InfrastructureResponse<bool>> ExistsByNameAsync(string name);
    }
}
