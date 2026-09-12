using Berryfy.Domain.Entities.InventoryEntities;
using Berryfy.Domain.Entities.ProductEntities;
using Berryfy.Domain.Repositories;
using Berryfy.Domain.Repositories.InventoryInterfaces;
using Berryfy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Berryfy.Infrastructure.Repositories.InventoryConcretes
{
    public class InventoryRepository : IInventoryRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork _unitOfWork;

        public InventoryRepository(ApplicationDbContext context, IUnitOfWork unitOfWork)
        {
            _context = context;
            _unitOfWork = unitOfWork;
        }

        public async Task<InventoryLog> CreateInventory(InventoryLog inventoryLog)
        {
            await _context.InventoryLogs.AddAsync(inventoryLog);
            if (await _unitOfWork.SaveDbChangesAsync())
            {
                return inventoryLog;
            }
            return null;
        }

        public async Task<List<InventoryLog>> GetInventoryHistoryAsync(int productId, int limit = 50)
        {
            return await _context.InventoryLogs
                .Where(i => i.ProductId == productId)
                .OrderByDescending(i => i.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }


        public async Task<bool> IsInStockAsync(int productId, int quantity)
        {
            if (quantity <= 0) return false;
            var product = await _context.Products.FindAsync(productId);
            return product != null && (product.StockQuantity - product.ReservedStock) >= quantity;
        }

        public async Task<bool> ReserveStockAsync(int productId, int quantity, int referenceId, string referenceType)
        {
            if (quantity <= 0) return false;
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return false;
            }

            int availableStock = product.StockQuantity - product.ReservedStock;
            if (availableStock < quantity)
            {
                return false;
            }

            product.ReservedStock += quantity;
            product.UpdatedAt = DateTime.UtcNow;

            return await _unitOfWork.SaveDbChangesAsync();
        }

        public async Task<bool> ReleaseReservedStockAsync(int productId, int quantity, int referenceId, string referenceType)
        {
            if (quantity <= 0) return false;
            var product = await _context.Products.FindAsync(productId);
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

            return await _unitOfWork.SaveDbChangesAsync();
        }

        public async Task<bool> ConfirmStockDeductionAsync(int productId, int quantity, int referenceId, string referenceType)
        {
            if (quantity <= 0) return false;
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return false;
            }

            if (product.ReservedStock < quantity || product.StockQuantity < quantity)
            {
                return false;
            }

            product.ReservedStock -= quantity;
            product.StockQuantity -= quantity;

            product.UpdatedAt = DateTime.UtcNow;

            return await _unitOfWork.SaveDbChangesAsync();
        }


        public async Task<bool> AddStockAsync(int productId, int quantity, string notes, int? performedByUserId)
        {
            if (quantity <= 0) return false;
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return false;
            }

            product.StockQuantity += quantity;
            product.UpdatedAt = DateTime.UtcNow;

            return await _unitOfWork.SaveDbChangesAsync();
        }

        public async Task<bool> AdjustStockAsync(int productId, int newQuantity, string notes, int? performedByUserId)
        {
            if (newQuantity < 0) return false;
            var product = await _context.Products.FindAsync(productId);
            if (product == null || newQuantity < product.ReservedStock)
            {
                return false;
            }

            product.StockQuantity = newQuantity;

            product.UpdatedAt = DateTime.UtcNow;

            return await _unitOfWork.SaveDbChangesAsync();
        }



        

        public async Task<List<Product>> GetLowStockProductsAsync(int limit = 50)
        {
            return await _context.Products
                .Where(p => p.StockQuantity <= p.LowStockThreshold)
                .OrderBy(p => p.StockQuantity)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<Product> GetProductWithStockInfoAsync(int productId)
        {
            return await _context.Products
                .Where(p => p.Id == productId)
                .Include(p => p.InventoryLogs.OrderByDescending(l => l.CreatedAt).Take(10))
                .FirstOrDefaultAsync();
        }
    }
}
