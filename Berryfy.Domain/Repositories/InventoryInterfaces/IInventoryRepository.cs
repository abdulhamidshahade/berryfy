using Berryfy.Domain.Entities;
using Berryfy.Domain.Entities.InventoryEntities;
using Berryfy.Domain.Entities.ProductEntities;

namespace Berryfy.Domain.Repositories.InventoryInterfaces
{
    public interface IInventoryRepository
    {
        Task<InfrastructureResponse<bool>> IsInStockAsync(int productId, int quantity);
        Task<InfrastructureResponse<bool>> ReserveStockAsync(int productId, int quantity, int referenceId, string referenceType);
        Task<InfrastructureResponse<bool>> ReleaseReservedStockAsync(int productId, int quantity, int referenceId, string referenceType);
        Task<InfrastructureResponse<bool>> ConfirmStockDeductionAsync(int productId, int quantity, int referenceId, string referenceType);
        Task<InfrastructureResponse<bool>> AddStockAsync(int productId, int quantity, string notes, int? performedByUserId);
        Task<InfrastructureResponse<bool>> AdjustStockAsync(int productId, int newQuantity, string notes, int? performedByUserId);
        Task<InfrastructureResponse<Product>> GetProductWithStockInfoAsync(int productId);
        Task<InfrastructureResponse<List<Product>>> GetLowStockProductsAsync(int limit = 50);
        Task<InfrastructureResponse<List<InventoryLog>>> GetInventoryHistoryAsync(int productId, int limit = 50);
        Task<InfrastructureResponse<InventoryLog>> CreateInventory(InventoryLog inventoryLog);

    }
}
