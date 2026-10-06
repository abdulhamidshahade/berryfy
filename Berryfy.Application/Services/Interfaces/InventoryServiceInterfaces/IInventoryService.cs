using Berryfy.Application.Dtos;
using Berryfy.Domain.Entities.InventoryEntities;
using Berryfy.Domain.Entities.ProductEntities;

namespace Berryfy.Application.Services.Interfaces.InventoryServiceInterfaces
{
    public interface IInventoryService
    {
        Task<ApplicationResponse<bool>> IsInStockAsync(int productId, int quantity);
        Task<ApplicationResponse<bool>> ReserveStockAsync(int productId, int quantity, int referenceId, string referenceType);
        Task<ApplicationResponse<bool>> ReleaseReservedStockAsync(int productId, int quantity, int referenceId, string referenceType);
        Task<ApplicationResponse<bool>> ConfirmStockDeductionAsync(int productId, int quantity, int referenceId, string referenceType);
        Task<ApplicationResponse<bool>> AddStockAsync(int productId, int quantity, string notes, int? performedByUserId);
        Task<ApplicationResponse<bool>> AdjustStockAsync(int productId, int newQuantity, string notes, int? performedByUserId);
        Task<ApplicationResponse<Product>> GetProductWithStockInfoAsync(int productId);
        Task<ApplicationResponse<List<Product>>> GetLowStockProductsAsync(int limit = 50);
        Task<ApplicationResponse<List<InventoryLog>>> GetInventoryHistoryAsync(int productId, int limit = 50);
        Task<ApplicationResponse<bool>> ProcessStockNotificationsAsync();
    }
}
