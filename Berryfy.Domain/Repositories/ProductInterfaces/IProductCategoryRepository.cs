using Berryfy.Domain.Entities;
using Berryfy.Domain.Entities.ProductEntities;

namespace Berryfy.Domain.Repositories.ProductInterfaces
{
    public interface IProductCategoryRepository
    {
        Task<InfrastructureResponse<bool>> AddProductCategoryAsync(Product Product, List<int> categories);
        Task<InfrastructureResponse<bool>> UpdateProductCategoryAsync(Product product, List<int> categories);
        Task<InfrastructureResponse<List<Category>>> GetCategoriesByProuductId(int productId);
        Task<InfrastructureResponse<bool>> RemoveCategoriesByProductId(int productId);
    }
}