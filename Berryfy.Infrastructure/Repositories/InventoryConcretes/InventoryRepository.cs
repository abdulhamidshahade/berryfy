using Berryfy.Domain.Constants;
using Berryfy.Domain.Entities;
using Berryfy.Domain.Entities.InventoryEntities;
using Berryfy.Domain.Entities.ProductEntities;
using Berryfy.Domain.Entities.ShoppingCartEntities;
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

        public async Task<InfrastructureResponse<InventoryLog>> CreateInventory(InventoryLog inventoryLog)
        {
            const string query = @"
                INSERT INTO inventory_logs (
                    product_id, current_stock_quantity, quantity_changed,
                    change_type, reference_id, reference_type, performed_by_user_id, notes, created_at, updated_at)
                VALUES (
                    @ProductId, @CurrentStockQuantity, @QuantityChanged,
                    @ChangeType, @ReferenceId, @ReferenceType, @PerformedByUserId, @Notes, @CreatedAt, @UpdatedAt)
                RETURNING *;";

            var now = DateTime.UtcNow;
            if (inventoryLog.CreatedAt == default) inventoryLog.CreatedAt = now;
            if (inventoryLog.UpdatedAt == default) inventoryLog.UpdatedAt = now;

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(query, connection);

            AddInventoryLogParameters(command, inventoryLog);

            InventoryLog? inventoryLogObj = null;
            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                inventoryLogObj = MapInventoryLog(reader);
            }

            return new InfrastructureResponse<InventoryLog>()
            {
                IsSuccess = inventoryLogObj != null,
                Message = "The process completed successfully",
                Value = inventoryLogObj!
            };
        }

        public async Task<InfrastructureResponse<List<InventoryLog>>> GetInventoryHistoryAsync(int productId, int limit = 50)
        {
            const string query = @"
                SELECT * FROM inventory_logs
                WHERE product_id = @ProductId
                ORDER BY created_at DESC
                LIMIT @limit;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@ProductId", productId);
            command.Parameters.AddWithValue("@limit", limit);

            List<InventoryLog> logs = new List<InventoryLog>();

            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                logs.Add(MapInventoryLog(reader));
            }

            return new InfrastructureResponse<List<InventoryLog>>()
            {
                IsSuccess = true,
                Message = "The process completed successfully",
                Value = logs
            };
        }

        public async Task<InfrastructureResponse<bool>> IsInStockAsync(int productId, int quantity)
        {
            const string productQuery = @"SELECT stock_quantity, reserved_stock FROM products WHERE id = @ProductId";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(productQuery, connection);
            command.Parameters.AddWithValue("@ProductId", productId);

            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return new InfrastructureResponse<bool>()
                {
                    IsSuccess = false,
                    Message = "Product not found",
                    Value = false
                };
            }

            int stockQuantity = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("stock_quantity")));
            int reservedQuantity = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("reserved_stock")));

            return new InfrastructureResponse<bool>()
            {
                IsSuccess = true,
                Message = "The process completed successfully",
                Value = quantity <= (stockQuantity - reservedQuantity)
            };
        }

        public async Task<InfrastructureResponse<bool>> ReserveStockAsync(
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

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@ProductId", productId);
            command.Parameters.AddWithValue("@Quantity", quantity);

            var rowsAffected = await command.ExecuteNonQueryAsync();

            return new InfrastructureResponse<bool>()
            {
                IsSuccess = true,
                Message = "The process completed successfully",
                Value = rowsAffected > 0
            };
        }

        public async Task<InfrastructureResponse<bool>> ReleaseReservedStockAsync(int productId, int quantity, int referenceId, string referenceType)
        {
            const string query = @"
                UPDATE products
                SET reserved_stock = GREATEST(0, reserved_stock - @quantity)
                WHERE id = @productId;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@productId", productId);
            command.Parameters.AddWithValue("@quantity", quantity);

            int rowEffected = await command.ExecuteNonQueryAsync();

            return new InfrastructureResponse<bool>()
            {
                IsSuccess = true,
                Message = "The process completed successfully",
                Value = rowEffected > 0
            };
        }

        public async Task<InfrastructureResponse<bool>> ConfirmStockDeductionAsync(int productId, int quantity, int referenceId, string referenceType)
        {
            const string query = @"
                UPDATE products
                SET stock_quantity = GREATEST(0, stock_quantity - @quantity),
                    reserved_stock = GREATEST(0, reserved_stock - @quantity)
                WHERE id = @ProductId;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@quantity", quantity);
            command.Parameters.AddWithValue("@ProductId", productId);

            int rowEffected = await command.ExecuteNonQueryAsync();
            return new InfrastructureResponse<bool>()
            {
                IsSuccess = true,
                Message = "The process completed successfully",
                Value = rowEffected > 0
            };
        }

        public async Task<InfrastructureResponse<bool>> AddStockAsync(int productId, int quantity, string notes, int? performedByUserId)
        {
            const string query = @"
                UPDATE products
                SET stock_quantity = stock_quantity + @quantity
                WHERE id = @productId;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@quantity", quantity);
            command.Parameters.AddWithValue("@productId", productId);

            int rowEffected = await command.ExecuteNonQueryAsync();

            return new InfrastructureResponse<bool>()
            {
                IsSuccess = true,
                Message = "The process completed successfully",
                Value = rowEffected > 0
            };
        }

        public async Task<InfrastructureResponse<bool>> AdjustStockAsync(int productId, int newQuantity, string notes, int? performedByUserId)
        {
            const string query = @"
                UPDATE products
                SET stock_quantity = @newQuantity
                WHERE id = @productId;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@newQuantity", newQuantity);
            command.Parameters.AddWithValue("@productId", productId);

            int rowEffected = await command.ExecuteNonQueryAsync();

            return new InfrastructureResponse<bool>()
            {
                IsSuccess = true,
                Message = "The process completed successfully",
                Value = rowEffected > 0
            };
        }

        public async Task<InfrastructureResponse<List<Product>>> GetLowStockProductsAsync(int limit = 50)
        {
            const string query = @"
                SELECT * FROM products
                WHERE (stock_quantity - reserved_stock) <= low_stock_threshold
                ORDER BY (stock_quantity - reserved_stock) ASC
                LIMIT @limit;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@limit", limit);

            List<Product> products = new List<Product>();

            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                products.Add(MapProduct(reader));
            }

            return new InfrastructureResponse<List<Product>>()
            {
                IsSuccess = true,
                Message = "The process completed successfully",
                Value = products
            };
        }

        public async Task<InfrastructureResponse<Product>> GetProductWithStockInfoAsync(int productId)
        {
            const string sql = @"
                SELECT p.id AS p_id, p.name, p.category_id, p.description, p.stock_quantity,
                       p.image_url, p.price, p.reserved_stock, p.low_stock_threshold, p.is_active, p.sku,
                       p.created_at AS p_created_at, p.updated_at AS p_updated_at,
                       il.id AS il_id, il.product_id, il.current_stock_quantity,
                       il.quantity_changed, il.change_type, il.reference_id, il.reference_type,
                       il.performed_by_user_id, il.notes, il.created_at AS il_created_at, il.updated_at AS il_updated_at
                FROM products p
                LEFT JOIN inventory_logs il ON (p.id = il.product_id)
                WHERE p.id = @ProductId
                ORDER BY il.created_at DESC
                LIMIT 20;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ProductId", productId);

            Dictionary<int, Product> product = new Dictionary<int, Product>();

            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                int id = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("p_id")));

                if (!product.TryGetValue(id, out Product? productObj))
                {
                    var descOrd = reader.GetOrdinal("description");
                    var imgOrd = reader.GetOrdinal("image_url");
                    var skuOrd = reader.GetOrdinal("sku");
                    var catOrd = reader.GetOrdinal("category_id");

                    productObj = new Product()
                    {
                        Id = id,
                        Name = reader.GetString(reader.GetOrdinal("name")),
                        Description = reader.IsDBNull(descOrd) ? string.Empty : reader.GetString(descOrd),
                        StockQuantity = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("stock_quantity"))),
                        ImageUrl = reader.IsDBNull(imgOrd) ? string.Empty : reader.GetString(imgOrd),
                        Price = reader.GetDecimal(reader.GetOrdinal("price")),
                        ReservedStock = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("reserved_stock"))),
                        LowStockThreshold = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("low_stock_threshold"))),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                        SKU = reader.IsDBNull(skuOrd) ? string.Empty : reader.GetString(skuOrd),
                        CategoryId = reader.IsDBNull(catOrd) ? 0 : Convert.ToInt32(reader.GetValue(catOrd)),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("p_created_at")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("p_updated_at")),
                        InventoryLogs = new List<InventoryLog>()
                    };

                    product.Add(id, productObj);
                }

                int ilIdOrd = reader.GetOrdinal("il_id");
                if (!reader.IsDBNull(ilIdOrd))
                {
                    var refIdOrd = reader.GetOrdinal("reference_id");
                    var refTypeOrd = reader.GetOrdinal("reference_type");
                    var notesOrd = reader.GetOrdinal("notes");
                    var perfOrd = reader.GetOrdinal("performed_by_user_id");

                    productObj.InventoryLogs.Add(new InventoryLog()
                    {
                        Id = Convert.ToInt32(reader.GetValue(ilIdOrd)),
                        ProductId = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("product_id"))),
                        CurrentStockQuantity = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("current_stock_quantity"))),
                        QuantityChanged = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("quantity_changed"))),
                        ChangeType = (InventoryChangeType)Convert.ToInt32(reader.GetValue(reader.GetOrdinal("change_type"))),
                        ReferenceId = reader.IsDBNull(refIdOrd) ? 0 : Convert.ToInt32(reader.GetValue(refIdOrd)),
                        ReferenceType = reader.IsDBNull(refTypeOrd) ? string.Empty : reader.GetString(refTypeOrd),
                        PerformedByUserId = reader.IsDBNull(perfOrd) ? null : Convert.ToInt32(reader.GetValue(perfOrd)),
                        Notes = reader.IsDBNull(notesOrd) ? string.Empty : reader.GetString(notesOrd),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("il_created_at")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("il_updated_at"))
                    });
                }
            }

            var found = product.Values.FirstOrDefault();
            return new InfrastructureResponse<Product>()
            {
                IsSuccess = found != null,
                Message = found != null ? "The process completed successfully" : "Product not found",
                Value = found!
            };
        }

        private async Task<NpgsqlConnection> OpenConnectionAsync()
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        private InventoryLog MapInventoryLog(NpgsqlDataReader reader)
        {
            var refIdOrd = reader.GetOrdinal("reference_id");
            var refTypeOrd = reader.GetOrdinal("reference_type");
            var notesOrd = reader.GetOrdinal("notes");
            var perfOrd = reader.GetOrdinal("performed_by_user_id");

            return new InventoryLog
            {
                Id = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("id"))),
                ProductId = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("product_id"))),
                CurrentStockQuantity = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("current_stock_quantity"))),
                QuantityChanged = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("quantity_changed"))),
                ChangeType = (InventoryChangeType)Convert.ToInt32(reader.GetValue(reader.GetOrdinal("change_type"))),
                ReferenceId = reader.IsDBNull(refIdOrd) ? 0 : Convert.ToInt32(reader.GetValue(refIdOrd)),
                ReferenceType = reader.IsDBNull(refTypeOrd) ? string.Empty : reader.GetString(refTypeOrd),
                PerformedByUserId = reader.IsDBNull(perfOrd) ? null : Convert.ToInt32(reader.GetValue(perfOrd)),
                Notes = reader.IsDBNull(notesOrd) ? string.Empty : reader.GetString(notesOrd),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
            };
        }

        private void AddInventoryLogParameters(NpgsqlCommand command, InventoryLog inventoryLog)
        {
            command.Parameters.AddWithValue("@ProductId", inventoryLog.ProductId);
            command.Parameters.AddWithValue("@CurrentStockQuantity", inventoryLog.CurrentStockQuantity);
            command.Parameters.AddWithValue("@QuantityChanged", inventoryLog.QuantityChanged);
            command.Parameters.AddWithValue("@ChangeType", (int)inventoryLog.ChangeType);
            command.Parameters.AddWithValue("@ReferenceId", inventoryLog.ReferenceId);
            command.Parameters.AddWithValue("@ReferenceType", (object?)inventoryLog.ReferenceType ?? string.Empty);
            command.Parameters.AddWithValue("@PerformedByUserId", (object?)inventoryLog.PerformedByUserId ?? DBNull.Value);
            command.Parameters.AddWithValue("@Notes", inventoryLog.Notes ?? string.Empty);
            command.Parameters.AddWithValue("@CreatedAt", inventoryLog.CreatedAt == default ? DateTime.UtcNow : inventoryLog.CreatedAt);
            command.Parameters.AddWithValue("@UpdatedAt", inventoryLog.UpdatedAt == default ? DateTime.UtcNow : inventoryLog.UpdatedAt);
        }

        private Product MapProduct(NpgsqlDataReader reader)
        {
            var descOrd = reader.GetOrdinal("description");
            var imgOrd = reader.GetOrdinal("image_url");
            var skuOrd = reader.GetOrdinal("sku");
            return new Product()
            {
                Id = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("id"))),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Description = reader.IsDBNull(descOrd) ? string.Empty : reader.GetString(descOrd),
                StockQuantity = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("stock_quantity"))),
                ImageUrl = reader.IsDBNull(imgOrd) ? string.Empty : reader.GetString(imgOrd),
                Price = reader.GetDecimal(reader.GetOrdinal("price")),
                ReservedStock = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("reserved_stock"))),
                LowStockThreshold = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("low_stock_threshold"))),
                IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                SKU = reader.IsDBNull(skuOrd) ? string.Empty : reader.GetString(skuOrd),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at")),
            };
        }

        public async Task<InfrastructureResponse<bool>> ReleaseItemsRelatedToCartId(int cartId, List<CartItem> cartItems)
        {
            foreach (var item in cartItems)
            {
                await ReleaseReservedStockAsync(item.ProductId, item.Quantity, cartId, "Releasing");
            }

            return new InfrastructureResponse<bool>()
            {
                IsSuccess = true,
                Value = true
            };
        }
    }
}