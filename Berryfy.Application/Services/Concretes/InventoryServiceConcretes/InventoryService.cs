using Berryfy.Application.Dtos;
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
        public InventoryService(IProductRepository productRepository,
                                IInventoryRepository inventoryRepository)
        {
            _productRepository = productRepository;
            _inventoryRepository = inventoryRepository;
        }

        public async Task<ApplicationResponse<bool>> IsInStockAsync(int productId, int quantity)
        {
            if (quantity <= 0)
            {
                return new ApplicationResponse<bool>
                {
                    IsSuccess = false,
                    Value = false
                };
            }

            var product = _productRepository.GetByIdAsync(productId).GetAwaiter().GetResult().Value;
            if (product == null)
            {
                return new ApplicationResponse<bool>
                {
                    IsSuccess = false,
                    Value = false
                };
            }

            var available = product.StockQuantity - product.ReservedStock;
            return new ApplicationResponse<bool>
            {
                IsSuccess = true,
                Value = available >= quantity
            };
        }

        public async Task<ApplicationResponse<bool>> AddStockAsync(int productId, int quantity, string notes, int? performedByUserId)
        {
            var product = _productRepository.GetByIdAsync(productId).GetAwaiter().GetResult().Value;

            if (product == null)
            {
                return new ApplicationResponse<bool>
                {
                    IsSuccess = false,
                    Value = false
                };
            }

            if (quantity <= 0)
            {
                return new ApplicationResponse<bool>
                {
                    IsSuccess = false,
                    Value = false
                };
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
                return new ApplicationResponse<bool>
                {
                    IsSuccess = false,
                    Value = false
                };
            }

            return new ApplicationResponse<bool>
            {
                IsSuccess = true,
                Value = true
            };
        }

        public async Task<ApplicationResponse<bool>> AdjustStockAsync(int productId, int newQuantity, string notes, int? performedByUserId)
        {
            var product = _productRepository.GetByIdAsync(productId).GetAwaiter().GetResult().Value;
            if (product == null)
            {
                return new ApplicationResponse<bool>
                {
                    IsSuccess = false,
                    Value = false
                };
            }

            if (newQuantity < 0 || newQuantity < product.ReservedStock)
            {
                return new ApplicationResponse<bool>
                {
                    IsSuccess = false,
                    Value = false
                };
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
                return new ApplicationResponse<bool>
                {
                    IsSuccess = false,
                    Value = false
                };
            }

            return new ApplicationResponse<bool>
            {
                IsSuccess = true,
                Value = true
            };
        }

        public async Task<ApplicationResponse<bool>> ConfirmStockDeductionAsync(int productId, int quantity, int referenceId, string referenceType)
        {
            if (quantity <= 0) return new ApplicationResponse<bool> { IsSuccess = false, Value = false };
            var product = _productRepository.GetByIdAsync(productId).GetAwaiter().GetResult().Value;

            if (product == null || product.ReservedStock < quantity || product.StockQuantity < quantity)
            {
                return new ApplicationResponse<bool>
                {
                    IsSuccess = false,
                    Value = false
                };
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

            return new ApplicationResponse<bool>
            {
                IsSuccess = createdInventory != null && updatedProduct != null,
                Value = createdInventory != null && updatedProduct != null
            };
        }


        public async Task<ApplicationResponse<List<InventoryLog>>> GetInventoryHistoryAsync(int productId, int limit = 50)
        {
            var inventoryHistory = _inventoryRepository.GetInventoryHistoryAsync(productId, limit).GetAwaiter().GetResult().Value;

            return new ApplicationResponse<List<InventoryLog>>
            {
                IsSuccess = true,
                Value = inventoryHistory
            };
        }

        public async Task<ApplicationResponse<List<Product>>> GetLowStockProductsAsync(int limit = 50)
        {
            var lowStockProducts = _inventoryRepository.GetLowStockProductsAsync(limit).GetAwaiter().GetResult().Value;
            return new ApplicationResponse<List<Product>>
            {
                IsSuccess = true,
                Value = lowStockProducts
            };
        }

        public async Task<ApplicationResponse<Product>> GetProductWithStockInfoAsync(int productId)
        {
            var product = _inventoryRepository.GetProductWithStockInfoAsync(productId).GetAwaiter().GetResult().Value;
            return new ApplicationResponse<Product>
            {
                IsSuccess = true,
                Value = product
            };
        }

        public async Task<ApplicationResponse<bool>> ProcessStockNotificationsAsync()
        {
            var lowStockProducts = GetLowStockProductsAsync(100).GetAwaiter().GetResult().Value;

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
            return new ApplicationResponse<bool>
            {
                IsSuccess = true,
                Value = true
            };
        }

        public async Task<ApplicationResponse<bool>> ReleaseReservedStockAsync(int productId, int quantity, int referenceId, string referenceType)
        {
            if (quantity <= 0) return new ApplicationResponse<bool> { IsSuccess = false, Value = false };
            var product = _productRepository.GetByIdAsync(productId).GetAwaiter().GetResult().Value;
            if (product == null)
            {
                return new ApplicationResponse<bool> { IsSuccess = false, Value = false };
            }

            if (product.ReservedStock < quantity)
            {
                return new ApplicationResponse<bool> { IsSuccess = false, Value = false };
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

            return new ApplicationResponse<bool>
            {
                IsSuccess = true,
                Value = createdInventory != null && updatedProduct != null
            };
        }

        public async Task<ApplicationResponse<bool>> ReserveStockAsync(int productId, int quantity, int referenceId, string referenceType)
        {
            if (quantity <= 0) return new ApplicationResponse<bool> { IsSuccess = false, Value = false };
            var product = _productRepository.GetByIdAsync(productId).GetAwaiter().GetResult().Value;
            if (product == null)
                return new ApplicationResponse<bool> { IsSuccess = false, Value = false };

            int availableStock = product.StockQuantity - product.ReservedStock;
            if (availableStock < quantity)
                return new ApplicationResponse<bool> { IsSuccess = false, Value = false };

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

            return new ApplicationResponse<bool>
            {
                IsSuccess = true,
                Value = createdInventory != null && updatedProduct != null
            };
        }
    }
}