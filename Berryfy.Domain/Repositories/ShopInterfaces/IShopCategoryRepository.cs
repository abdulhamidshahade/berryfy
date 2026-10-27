//using Berryfy.Domain.Entities;
//using Berryfy.Domain.Entities.ProductEntities;
//using Berryfy.Domain.Entities.ShopEntities;

//namespace Berryfy.Domain.Repositories.ShopInterfaces
//{
//    public interface IShopCategoryRepository
//    {
//        Task<InfrastructureResponse<IEnumerable<ShopCategory>>> GetAllByShopIdAsync(int shopId, bool includeInactive);
//        Task<InfrastructureResponse<ShopCategory?>> GetByIdAsync(int shopId, int id);
//        Task<InfrastructureResponse<ShopCategory?>> GetBySlugAsync(int shopId, string slug);
//        Task<InfrastructureResponse<ShopCategory>> CreateAsync(ShopCategory category);
//        Task<InfrastructureResponse<ShopCategory?>> UpdateAsync(ShopCategory category);
//        Task<InfrastructureResponse<bool>> DeleteAsync(int shopId, int id);
//        Task<InfrastructureResponse<bool>> ExistsByIdAsync(int shopId, int id);
//        Task<InfrastructureResponse<bool>> ExistsBySlugAsync(int shopId, string slug, int? excludeId = null);
//        Task<InfrastructureResponse<bool>> HasChildCategoriesAsync(int shopId, int id);
//        Task<InfrastructureResponse<bool>> IsSelfOrDescendantAsync(int shopId, int ancestorId, int candidateId);
//        Task<InfrastructureResponse<List<Product>>> GetProductsByShopCategoryIdAsync(int shopId, int id);
//        Task<InfrastructureResponse<int>> GetProductCountByShopCategoryIdAsync(int shopId, int id);
//    }
//}