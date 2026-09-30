using Berryfy.Domain.Constants;
using Berryfy.Domain.Entities.InventoryEntities;
using Berryfy.Domain.Entities.ProductEntities;
using Berryfy.Domain.Repositories.InventoryInterfaces;
using Berryfy.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Berryfy.Infrastructure.Repositories.InventoryConcretes
{
    public class InventoryRepository : IInventoryRepository
    {
        private readonly string _connectionString;

        public InventoryRepository(IConfiguration config)
        {
            _connectionString = PostgresConnectionStrings.Resolve(config);
        }

        public async Task<InventoryLog> CreateInventory(InventoryLog inventoryLog)
        {
            string query = @"Insert into InventoryLogs (product_id, current_stock_quantity, quantity_changed,
change_type, reference_id, reference_type, performed_by_user_id, notes, created_at, updated_at) values (@ProductId,
@CurrentStockQuantity, @QuantityChanged, @ChangeType, @ReferenceId, @ReferenceType, @PerformedByUserId, @Notes, @CreatedAt,
@UpdatedAt) Returning *;";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(query, connection);

            AddInventoryLogParameters(command, inventoryLog);

            InventoryLog inventoryLogObj = new InventoryLog();

            var reader = await command.ExecuteReaderAsync();

            if(await reader.ReadAsync())
            {
                inventoryLogObj = MapInventoryLog(reader);
            }

            return inventoryLogObj;
        }

        public async Task<List<InventoryLog>> GetInventoryHistoryAsync(int productId, int limit = 50)
        {
            string query = @"select * from inventory_logs where product_id = @ProductId limit @limit";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@ProductId", productId);
            command.Parameters.AddWithValue("@limit", limit);

            List<InventoryLog> logs = new List<InventoryLog>();

            var reader = await command.ExecuteReaderAsync();

            while(await reader.ReadAsync())
            {
                logs.Add(MapInventoryLog(reader));
            }

            return logs;
        }


        public async Task<bool> IsInStockAsync(int productId, int quantity)
        {
            string productQuery = @"select stock_quantity, reserved_stock from products where id = @ProductId";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(productQuery, connection);

            command.Parameters.AddWithValue("@ProductId", productId);

            var reader = await command.ExecuteReaderAsync();

            int stockQuantity = 0;
            int reservedQuantity = 0;

            while (await reader.ReadAsync())
            {
                stockQuantity = reader.GetInt32(reader.GetOrdinal("stock_quantity"));
                reservedQuantity = reader.GetInt32(reader.GetOrdinal("reserved_stock"));
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
        UPDATE products
        SET reserved_stock = reserved_stock + @Quantity
        WHERE id = @ProductId
          AND stock_quantity - reserved_stock >= @Quantity;
    ";

            var connection = await OpenConnectionAsync();

            await using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@ProductId", productId);
            command.Parameters.AddWithValue("@Quantity", quantity);

            var rowsAffected = await command.ExecuteNonQueryAsync();

            return rowsAffected > 0;
        }

        public async Task<bool> ReleaseReservedStockAsync(int productId, int quantity, int referenceId, string referenceType)
        {
            string query = @"Update products
Set reserved_stock = reserved_stock - @quantity
where id = @productId;";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@productId", productId);
            command.Parameters.AddWithValue("@quantity", quantity);

            int rowEffected = await command.ExecuteNonQueryAsync();

            return rowEffected > 0;
        }

        public async Task<bool> ConfirmStockDeductionAsync(int productId, int quantity, int referenceId, string referenceType)
        {
            string query = @"Update products
Set stock_quantity = stock_quantity - @quantity,
reserved_stock = reserved_stock - @quantity
where id = @ProductId;";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@quantity", quantity);
            command.Parameters.AddWithValue("@ProductId", productId);

            int rowEffected = await command.ExecuteNonQueryAsync();
            return rowEffected > 0;
        }


        public async Task<bool> AddStockAsync(int productId, int quantity, string notes, int? performedByUserId)
        {
            string query = @"Update products
Set stock_quantity = stock_quantity + @quantity
where id = @productId;";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@quantity", quantity);
            command.Parameters.AddWithValue("@productId", productId);

            int rowEffected = await command.ExecuteNonQueryAsync();

            return rowEffected > 0;
        }

        public async Task<bool> AdjustStockAsync(int productId, int newQuantity, string notes, int? performedByUserId)
        {
            string query = @"Update products
set stock_quantity = @newQuantity
where id = @productId;";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@newQuantity", newQuantity);
            command.Parameters.AddWithValue("@productId", productId);

            int rowEffected = await command.ExecuteNonQueryAsync();

            return rowEffected > 0;
        }

        public async Task<List<Product>> GetLowStockProductsAsync(int limit = 50)
        {
            string query = @"Select * from products
where stock_quantity < low_stock_threshold
order by stock_quantity
limit @limit;";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@limit", limit);

            List<Product> products = new List<Product>();

            var reader = await command.ExecuteReaderAsync();

            while(await reader.ReadAsync())
            {
                products.Add(MapProduct(reader));
            }

            return products;
        }

        public async Task<Product> GetProductWithStockInfoAsync(int productId)
        {
            string sql = @"select p.*, il.* from products p
left join InventoryLogs il on (p.id = il.product_id)
where p.id = @productId
order by il.created_at desc
limit 20;";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@productId", productId);

            Dictionary<int, Product> product = new Dictionary<int, Product>();

            var reader = await command.ExecuteReaderAsync();

            while(await reader.ReadAsync())
            {
                int id = reader.GetInt16(reader.GetOrdinal("id"));

                if(product.TryGetValue(id, out Product productObj))
                {
                    productObj = MapProduct(reader);
                }
                productObj.InventoryLogs.Add(MapInventoryLog(reader));
            }

            return product.Values.FirstOrDefault();
        }

        private async Task<NpgsqlConnection> OpenConnectionAsync()
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            return connection;
        }

        private InventoryLog MapInventoryLog(NpgsqlDataReader reader)
        {
            return new InventoryLog
            {
                Id = reader.GetInt16(reader.GetOrdinal("Id")),
                ProductId = reader.GetInt16(reader.GetOrdinal("product_id")),
                CurrentStockQuantity = reader.GetInt16(reader.GetOrdinal("current_stock_quantity")),
                QuantityChanged = reader.GetInt16(reader.GetOrdinal("quantity_changed")),
                ChangeType = (InventoryChangeType)reader.GetValue(reader.GetOrdinal("change_type")),
                ReferenceId = reader.GetInt16(reader.GetOrdinal("reference_id")),
                ReferenceType = reader.GetString(reader.GetOrdinal("reference_type")),
                PerformedByUserId = reader.GetInt16(reader.GetOrdinal("performed_by_user_id")),
                Notes = reader.GetString(reader.GetOrdinal("notes")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
            };
        }

        private void AddInventoryLogParameters(NpgsqlCommand command, InventoryLog inventoryLog)
        {
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
        }

        private Product MapProduct(NpgsqlDataReader reader)
        {
            return new Product()
            {
                Id = reader.GetInt16(reader.GetOrdinal("id")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Description = reader.GetString(reader.GetOrdinal("description")),
                StockQuantity = reader.GetInt16(reader.GetOrdinal("stock_quantity")),
                ImageUrl = reader.GetString(reader.GetOrdinal("image_url")),
                Price = reader.GetDecimal(reader.GetOrdinal("price")),
                ReservedStock = reader.GetInt16(reader.GetOrdinal("reserved_stock")),
                LowStockThreshold = reader.GetInt16(reader.GetOrdinal("low_stock_threshold")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                SKU = reader.GetString(reader.GetOrdinal("sku")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at")),
            };
            
        }
    }
}