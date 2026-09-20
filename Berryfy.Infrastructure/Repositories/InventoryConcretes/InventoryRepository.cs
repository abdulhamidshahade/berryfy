using Berryfy.Domain.Constants;
using Berryfy.Domain.Entities.InventoryEntities;
using Berryfy.Domain.Entities.ProductEntities;
using Berryfy.Domain.Repositories.InventoryInterfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Berryfy.Infrastructure.Repositories.InventoryConcretes
{
    public class InventoryRepository : IInventoryRepository
    {
        private readonly string _connectionString;

        public InventoryRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("PostgreSQLServer");
        }

        public async Task<InventoryLog> CreateInventory(InventoryLog inventoryLog)
        {
            string query = @"Insert into InventoryLogs (ProductId, CurrentStockQuantity, QuantityChanged,
ChangeType, ReferenceId, ReferenceType, PerformedByUserId, Notes, CreatedAt, UpdatedAt) values (@ProductId,
@CurrentStockQuantity, @QuantityChanged, @ChangeType, @ReferenceId, @ReferenceType, @PerformedByUserId, @Notes, @CreatedAt,
@UpdatedAt) Returning *;";

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@ProductId", inventoryLog.ProductId);
            command.Parameters.AddWithValue("@CurrentStockQuantity", inventoryLog.CurrentStockQuantity);
            command.Parameters.AddWithValue("@QuantityChanged", inventoryLog.QuantityChanged);
            command.Parameters.AddWithValue("@ChangeType", inventoryLog.ChangeType);
            command.Parameters.AddWithValue("@ReferenceId", inventoryLog.ReferenceId);
            command.Parameters.AddWithValue("@ReferenceType", inventoryLog.ReferenceType);
            command.Parameters.AddWithValue("@PerformedByUserId", inventoryLog.PerformedByUserId);
            command.Parameters.AddWithValue("@Notes", inventoryLog.Notes);
            command.Parameters.AddWithValue("@CreatedAt", inventoryLog.CreatedAt);
            command.Parameters.AddWithValue("@UpdatedAt", inventoryLog.UpdatedAt);


            InventoryLog inventoryLogObj = null;

            var reader = await command.ExecuteReaderAsync();

            while(await reader.ReadAsync())
            {
                inventoryLogObj = new InventoryLog
                {
                    Id = reader.GetInt16(reader.GetOrdinal("Id")),
                    ProductId = reader.GetInt16(reader.GetOrdinal("ProductId")),
                    CurrentStockQuantity = reader.GetInt16(reader.GetOrdinal("CurrentStockQuantity")),
                    QuantityChanged = reader.GetInt16(reader.GetOrdinal("QuantityChanged")),
                    ChangeType = (InventoryChangeType)reader.GetValue(reader.GetOrdinal("ChangeType")),
                    ReferenceId = reader.GetInt16(reader.GetOrdinal("ReferenceId")),
                    ReferenceType = reader.GetString(reader.GetOrdinal("ReferenceType")),
                    PerformedByUserId = reader.GetInt16(reader.GetOrdinal("PerformedByUserId")),
                    Notes = reader.GetString(reader.GetOrdinal("Notes")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
                };
            }

            return inventoryLogObj;
        }

        public async Task<List<InventoryLog>> GetInventoryHistoryAsync(int productId, int limit = 50)
        {
            string query = @"select * from InventoryLogs where ProductId = @ProductId limit @limit";

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@ProductId", productId);
            command.Parameters.AddWithValue("@limit", limit);

            List<InventoryLog> logs = new List<InventoryLog>();

            var reader = await command.ExecuteReaderAsync();

            while(await reader.ReadAsync())
            {
                logs.Add(new InventoryLog
                {
                    Id = reader.GetInt16(reader.GetOrdinal("Id")),
                    ProductId = reader.GetInt16(reader.GetOrdinal("ProductId")),
                    CurrentStockQuantity = reader.GetInt16(reader.GetOrdinal("CurrentStockQuantity")),
                    QuantityChanged = reader.GetInt16(reader.GetOrdinal("QuantityChanged")),
                    ChangeType = (InventoryChangeType)reader.GetValue(reader.GetOrdinal("ChangeType")),
                    ReferenceId = reader.GetInt16(reader.GetOrdinal("ReferenceId")),
                    ReferenceType = reader.GetString(reader.GetOrdinal("ReferenceType")),
                    PerformedByUserId = reader.GetInt16(reader.GetOrdinal("PerformedByUserId")),
                    Notes = reader.GetString(reader.GetOrdinal("Notes")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
                });
            }

            return logs;
        }


        public async Task<bool> IsInStockAsync(int productId, int quantity)
        {
            string productQuery = @"select StockQuantity, ReservedStock from Products where Id = @ProductId";

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(productQuery, connection);

            command.Parameters.AddWithValue("@ProductId", productId);

            var reader = await command.ExecuteReaderAsync();

            int stockQuantity = 0;
            int reservedQuantity = 0;

            while (await reader.ReadAsync())
            {
                stockQuantity = reader.GetInt32(reader.GetOrdinal("StockQuantity"));
                reservedQuantity = reader.GetInt32(reader.GetOrdinal("ReservedQuantity"));
            }

            return (quantity <= (stockQuantity - reservedQuantity));
            
        }

        public async Task<bool> ReserveStockAsync(
    int productId,
    int quantity,
    int referenceId,
    string referenceType)
        {
            const string query = @"
        UPDATE Products
        SET ReservedQuantity = ReservedQuantity + @Quantity
        WHERE Id = @ProductId
          AND StockQuantity - ReservedQuantity >= @Quantity;
    ";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@ProductId", productId);
            command.Parameters.AddWithValue("@Quantity", quantity);

            var rowsAffected = await command.ExecuteNonQueryAsync();

            return rowsAffected > 0;
        }

        public async Task<bool> ReleaseReservedStockAsync(int productId, int quantity, int referenceId, string referenceType)
        {
            string query = @"Update Products
Set ReservedStock = ReservedStock - @quantity
where Id = @productId;";

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@productId", productId);
            command.Parameters.AddWithValue("@quantity", quantity);

            int rowEffected = await command.ExecuteNonQueryAsync();

            return rowEffected > 0;
        }

        public async Task<bool> ConfirmStockDeductionAsync(int productId, int quantity, int referenceId, string referenceType)
        {
            string query = @"Update Products
Set StockQuantity = StockQuantity - @quantity,
ReservedStock = ReservedStock - @quantity
where Id = @ProductId;";

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@quantity", quantity);
            command.Parameters.AddWithValue("@ProductId", productId);

            int rowEffected = await command.ExecuteNonQueryAsync();
            return rowEffected > 0;
        }


        public async Task<bool> AddStockAsync(int productId, int quantity, string notes, int? performedByUserId)
        {
            string query = @"Update Products
Set StockQuantity = StockQuantity + @quantity
where Id = @productId;";

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@quantity", quantity);
            command.Parameters.AddWithValue("@productId", productId);

            int rowEffected = await command.ExecuteNonQueryAsync();

            return rowEffected > 0;
        }

        public async Task<bool> AdjustStockAsync(int productId, int newQuantity, string notes, int? performedByUserId)
        {
            string query = @"Update Products
set StockQuantity = @newQuantity
where Id = @productId;";

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@newQuantity", newQuantity);
            command.Parameters.AddWithValue("@productId", productId);

            int rowEffected = await command.ExecuteNonQueryAsync();

            return rowEffected > 0;
        }



        

        public async Task<List<Product>> GetLowStockProductsAsync(int limit = 50)
        {
            string query = @"Select * from Products
where StockQuantity < LowStockThreshold
order by StockQuantity
limit @limit;";

            var connection = new NpgsqlConnection(_connectionString);

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@limit", limit);

            List<Product> products = new List<Product>();

            var reader = await command.ExecuteReaderAsync();

            while(await reader.ReadAsync())
            {
                products.Add(new Product
                {
                    Id = reader.GetInt16(reader.GetOrdinal("Id")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Description = reader.GetString(reader.GetOrdinal("Description")),
                    StockQuantity = reader.GetInt16(reader.GetOrdinal("StockQuantity")),
                    ImageUrl = reader.GetString(reader.GetOrdinal("ImageUrl")),
                    Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                    ReservedStock = reader.GetInt16(reader.GetOrdinal("ReservedStock")),
                    LowStockThreshold = reader.GetInt16(reader.GetOrdinal("LowStockThreshold")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                    SKU = reader.GetString(reader.GetOrdinal("SKU")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
                });
            }

            return products;
        }

        public async Task<Product> GetProductWithStockInfoAsync(int productId)
        {
            string sql = @"select p.*, il.* from products p
left join InventoryLogs il on (p.Id = il.ProductId)
where p.Id = @productId
order by il.CreatedAt desc
limit 20;";

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@productId", productId);

            Dictionary<int, Product> product = new Dictionary<int, Product>();

            var reader = await command.ExecuteReaderAsync();

            while(await reader.ReadAsync())
            {
                int id = reader.GetInt16(reader.GetOrdinal("Id"));

                if(product.TryGetValue(id, out Product productObj))
                {
                    productObj = new Product
                    {
                        Id = id,
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        Description = reader.GetString(reader.GetOrdinal("Description")),
                        StockQuantity = reader.GetInt16(reader.GetOrdinal("StockQuantity")),
                        ImageUrl = reader.GetString(reader.GetOrdinal("ImageUrl")),
                        Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                        ReservedStock = reader.GetInt16(reader.GetOrdinal("ReservedStock")),
                        LowStockThreshold = reader.GetInt16(reader.GetOrdinal("LowStockThreshold")),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                        SKU = reader.GetString(reader.GetOrdinal("SKU")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
                    };
                }
                productObj.InventoryLogs.Add(new InventoryLog
                {
                    ProductId = reader.GetInt16(reader.GetOrdinal("ProductId")),
                    CurrentStockQuantity = reader.GetInt16(reader.GetOrdinal("CurrentStockQuantity")),
                    QuantityChanged = reader.GetInt16(reader.GetOrdinal("QuantityChanged")),
                    ChangeType = (InventoryChangeType)reader.GetValue(reader.GetOrdinal("ChangeType")),
                    ReferenceId = reader.GetInt32(reader.GetOrdinal("ReferenceId")),
                    ReferenceType = reader.GetString(reader.GetOrdinal("ReferenceType")),
                    PerformedByUserId = reader.GetInt16(reader.GetOrdinal("PerformedByUserId")),
                    Notes = reader.GetString(reader.GetOrdinal("Notes"))
                });
            }

            return product.Values.FirstOrDefault();
        }
    }
}
