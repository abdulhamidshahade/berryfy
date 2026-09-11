using Berryfy.Application.Services.Interfaces.InventoryServiceInterfaces;
using Berryfy.Domain.Constants;
using Berryfy.Domain.Entities.InventoryEntities;
using Berryfy.Domain.Entities.ProductEntities;
using Berryfy.Domain.Repositories;
using Berryfy.Domain.Repositories.InventoryInterfaces;
using Berryfy.Domain.Repositories.ProductInterfaces;

namespace Berryfy.Application.Services.Concretes.InventoryServiceConcretes
{
    public class InventoryService : IInventoryService
    {
        private readonly IProductRepository _productRepository;
        private readonly IInventoryRepository _inventoryRepository;
        private readonly IUnitOfWork _unitOfWork;

        public InventoryService(IProductRepository productRepository,
                                IInventoryRepository inventoryRepository,
                                IUnitOfWork unitOfWork)
        {
            _productRepository = productRepository;
            _inventoryRepository = inventoryRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> IsInStockAsync(int productId, int quantity)
        {
            if (quantity <= 0)
            {
                return false;
            }

            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
            {
                return false;
            }

            var available = product.StockQuantity - product.ReservedStock;
            return available >= quantity;
        }

        public async Task<bool> AddStockAsync(int productId, int quantity, string notes, int? performedByUserId)
        {
            var product = await _productRepository.GetByIdAsync(productId);

            if (product == null)
            {
                throw new KeyNotFoundException($"Product with ID {productId} not found");
            }

            if (quantity <= 0)
            {
                return false;
            }

            product.StockQuantity += quantity;
            product.UpdatedAt = DateTime.UtcNow;

            var updatedProduct = await _productRepository.UpdateAsync(productId, product);


            var inventoryLog = new InventoryLog
            {
                ProductId = productId,
                CurrentStockQuantity = product.StockQuantity,
                QuantityChanged = quantity,
                ChangeType = InventoryChangeType.Restock,
                Notes = notes,
                ReferenceId = 1409,
                ReferenceType = "Stock Addition",
                CreatedAt = DateTime.UtcNow,
                PerformedByUserId = performedByUserId
            };

            var createdInventory = await _inventoryRepository.CreateInventory(inventoryLog);

            if (createdInventory == null)
            {
                return false;
            }

            return true;
        }

        public async Task<bool> AdjustStockAsync(int productId, int newQuantity, string notes, int? performedByUserId)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
            {
                return false;
            }

            if (newQuantity < 0 || newQuantity < product.ReservedStock)
            {
                return false;
            }

            int difference = newQuantity - product.StockQuantity;

            product.StockQuantity = newQuantity;

            product.UpdatedAt = DateTime.UtcNow;

            var inventoryLog = new InventoryLog
            {
                ProductId = productId,
                CurrentStockQuantity = product.StockQuantity,
                QuantityChanged = difference,
                ChangeType = InventoryChangeType.StockAdjustment,
                Notes = notes,
                CreatedAt = DateTime.UtcNow,
                PerformedByUserId = performedByUserId
            };

            var createdInventory = await _inventoryRepository.CreateInventory(inventoryLog);

            if (createdInventory == null)
            {
                return false;
            }

            return true;
        }

        public async Task<bool> ConfirmStockDeductionAsync(int productId, int quantity, int referenceId, string referenceType)
        {
            if (quantity <= 0) return false;
            var product = await _productRepository.GetByIdAsync(productId);

            if (product == null || product.ReservedStock < quantity || product.StockQuantity < quantity)
            {
                return false;
            }

            product.ReservedStock -= quantity;
            product.StockQuantity -= quantity;
            product.UpdatedAt = DateTime.UtcNow;

            var inventoryLog = new InventoryLog
            {
                ProductId = productId,
                CurrentStockQuantity = product.StockQuantity,
                QuantityChanged = -quantity,
                ChangeType = InventoryChangeType.Purchase,
                ReferenceId = referenceId,
                ReferenceType = referenceType,
                Notes = $"Purchased {quantity} units via {referenceType} {referenceId}",
                CreatedAt = DateTime.UtcNow
            };

            var updatedProduct = await _productRepository.UpdateAsync(product.Id, product);
            var createdInventory = await _inventoryRepository.CreateInventory(inventoryLog);

            return createdInventory != null && updatedProduct != null;
        }


        public async Task<List<InventoryLog>> GetInventoryHistoryAsync(int productId, int limit = 50)
        {
            var inventoryHistory = await _inventoryRepository.GetInventoryHistoryAsync(productId, limit);

            return inventoryHistory;
        }

        public async Task<List<Product>> GetLowStockProductsAsync(int limit = 50)
        {
            return await _inventoryRepository.GetLowStockProductsAsync(limit);
        }

        public async Task<Product> GetProductWithStockInfoAsync(int productId)
        {
            return await _inventoryRepository.GetProductWithStockInfoAsync(productId);
        }

        public async Task ProcessStockNotificationsAsync()
        {
            var lowStockProducts = await GetLowStockProductsAsync(100);

            foreach (var product in lowStockProducts)
            {
                var inventoryLog = new InventoryLog
                {
                    ProductId = product.Id,
                    CurrentStockQuantity = product.StockQuantity,
                    QuantityChanged = 0,
                    ChangeType = InventoryChangeType.StockAdjustment,
                    Notes = $"Low stock notification: {product.Name} has {product.StockQuantity} units (threshold: {product.LowStockThreshold})",
                    CreatedAt = DateTime.UtcNow
                };

                await _inventoryRepository.CreateInventory(inventoryLog);
            }
        }

        public async Task<bool> ReleaseReservedStockAsync(int productId, int quantity, int referenceId, string referenceType)
        {
            if (quantity <= 0) return false;
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
            {
                return false;
            }

            if (product.ReservedStock < quantity)
            {
                return false;
            }

            product.ReservedStock -= quantity;
            product.UpdatedAt = DateTime.UtcNow;

            var inventoryLog = new InventoryLog
            {
                ProductId = productId,
                CurrentStockQuantity = product.StockQuantity,
                QuantityChanged = 0,
                ChangeType = InventoryChangeType.ReleaseReservation,
                ReferenceId = referenceId,
                ReferenceType = referenceType,
                Notes = $"Released {quantity} units from {referenceType} {referenceId}",
                CreatedAt = DateTime.UtcNow
            };

            var updatedProduct = await _productRepository.UpdateAsync(product.Id, product);
            var createdInventory = await _inventoryRepository.CreateInventory(inventoryLog);

            return createdInventory != null && updatedProduct != null;
        }

        public async Task<bool> ReserveStockAsync(int productId, int quantity, int referenceId, string referenceType)
        {
            if (quantity <= 0) return false;
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
                return false;

            int availableStock = product.StockQuantity - product.ReservedStock;
            if (availableStock < quantity)
                return false;

            product.ReservedStock += quantity;
            product.UpdatedAt = DateTime.UtcNow;

            var inventoryLog = new InventoryLog
            {
                ProductId = productId,
                CurrentStockQuantity = product.StockQuantity,
                QuantityChanged = 0,
                ChangeType = InventoryChangeType.Reserved,
                ReferenceId = referenceId,
                ReferenceType = referenceType,
                Notes = $"Reserved {quantity} units for {referenceType} {referenceId}",
                CreatedAt = DateTime.UtcNow
            };

            var updatedProduct = await _productRepository.UpdateAsync(product.Id, product);
            var createdInventory = await _inventoryRepository.CreateInventory(inventoryLog);

            return createdInventory != null && updatedProduct != null;
        }


    }
}
