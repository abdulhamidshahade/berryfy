using Berryfy.Domain.Entities;
using Berryfy.Domain.Entities.ProductEntities;
using Berryfy.Domain.Entities.ShopEntities;
using Berryfy.Domain.Repositories.ShopInterfaces;
using Berryfy.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Berryfy.Infrastructure.Repositories.ShopConcretes
{
    public class ShopCategoryRepository : IShopCategoryRepository
    {
        private const string SelectColumns = @"
            sc.id, sc.shop_id, sc.name, sc.description, sc.image_url, sc.slug,
            sc.parent_category_id, sc.is_active, sc.display_order, sc.created_at, sc.updated_at";

        private const string ReturningColumns = @"
            id, shop_id, name, description, image_url, slug,
            parent_category_id, is_active, display_order, created_at, updated_at";

        private readonly string _connectionString;

        public ShopCategoryRepository(IConfiguration config)
        {
            _connectionString = PostgresConnectionStrings.Resolve(config);
        }

        public async Task<InfrastructureResponse<IEnumerable<ShopCategory>>> GetAllByShopIdAsync(int shopId, bool includeInactive)
        {
            var sql = $@"
                SELECT {SelectColumns}
                FROM shop_categories sc
                WHERE sc.shop_id = @ShopId
                  AND (@IncludeInactive OR sc.is_active = TRUE)
                ORDER BY sc.display_order, sc.name, sc.id;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ShopId", (long)shopId);
            command.Parameters.AddWithValue("@IncludeInactive", includeInactive);

            await using var reader = await command.ExecuteReaderAsync();

            var categories = new List<ShopCategory>();
            while (await reader.ReadAsync())
            {
                categories.Add(MapShopCategory(reader));
            }

            return Success<IEnumerable<ShopCategory>>(categories, "Shop categories retrieved successfully");
        }

        public async Task<InfrastructureResponse<ShopCategory?>> GetByIdAsync(int shopId, int id)
        {
            var sql = $@"
                SELECT {SelectColumns}
                FROM shop_categories sc
                WHERE sc.shop_id = @ShopId AND sc.id = @Id;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ShopId", (long)shopId);
            command.Parameters.AddWithValue("@Id", (long)id);

            return Success(await ReadSingleAsync(command), "Shop category retrieved successfully");
        }

        public async Task<InfrastructureResponse<ShopCategory?>> GetBySlugAsync(int shopId, string slug)
        {
            var sql = $@"
                SELECT {SelectColumns}
                FROM shop_categories sc
                WHERE sc.shop_id = @ShopId AND sc.slug = @Slug;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ShopId", (long)shopId);
            command.Parameters.AddWithValue("@Slug", slug);

            return Success(await ReadSingleAsync(command), "Shop category retrieved successfully");
        }

        public async Task<InfrastructureResponse<ShopCategory>> CreateAsync(ShopCategory category)
        {
            var sql = $@"
                INSERT INTO shop_categories
                    (shop_id, name, description, image_url, slug, parent_category_id, is_active, display_order)
                VALUES
                    (@ShopId, @Name, @Description, @ImageUrl, @Slug, @ParentCategoryId, @IsActive, @DisplayOrder)
                RETURNING {ReturningColumns};";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            AddShopCategoryParameters(command, category);

            ShopCategory? created;
            try
            {
                created = await ReadSingleAsync(command);
            }
            catch (PostgresException ex) when (TryMapConstraintViolation(ex, out var marker))
            {
                return new InfrastructureResponse<ShopCategory> { IsSuccess = false, Message = marker, Value = null! };
            }

            return new InfrastructureResponse<ShopCategory>
            {
                IsSuccess = created != null,
                Message = created != null ? "Shop category created successfully" : "Shop category could not be created",
                Value = created!
            };
        }

        public async Task<InfrastructureResponse<ShopCategory?>> UpdateAsync(ShopCategory category)
        {
            var sql = $@"
                UPDATE shop_categories
                SET name = @Name,
                    description = @Description,
                    image_url = @ImageUrl,
                    slug = @Slug,
                    parent_category_id = @ParentCategoryId,
                    is_active = @IsActive,
                    display_order = @DisplayOrder,
                    updated_at = NOW()
                WHERE shop_id = @ShopId AND id = @Id
                RETURNING {ReturningColumns};";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            AddShopCategoryParameters(command, category);
            command.Parameters.AddWithValue("@Id", (long)category.Id);

            ShopCategory? updated;
            try
            {
                updated = await ReadSingleAsync(command);
            }
            catch (PostgresException ex) when (TryMapConstraintViolation(ex, out var marker))
            {
                return new InfrastructureResponse<ShopCategory?> { IsSuccess = false, Message = marker, Value = null };
            }

            return new InfrastructureResponse<ShopCategory?>
            {
                IsSuccess = updated != null,
                Message = updated != null ? "Shop category updated successfully" : "Shop category not found",
                Value = updated
            };
        }

        public async Task<InfrastructureResponse<bool>> DeleteAsync(int shopId, int id)
        {
            const string sql = "DELETE FROM shop_categories WHERE shop_id = @ShopId AND id = @Id;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ShopId", (long)shopId);
            command.Parameters.AddWithValue("@Id", (long)id);

            int rowsAffected;
            try
            {
                rowsAffected = await command.ExecuteNonQueryAsync();
            }
            catch (PostgresException ex) when (TryMapConstraintViolation(ex, out var marker))
            {
                return new InfrastructureResponse<bool> { IsSuccess = false, Message = marker, Value = false };
            }

            return Success(rowsAffected > 0, rowsAffected > 0 ? "Shop category deleted successfully" : "Shop category not found");
        }

        public async Task<InfrastructureResponse<bool>> ExistsByIdAsync(int shopId, int id)
        {
            const string sql = "SELECT EXISTS (SELECT 1 FROM shop_categories WHERE shop_id = @ShopId AND id = @Id);";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ShopId", (long)shopId);
            command.Parameters.AddWithValue("@Id", (long)id);

            var exists = (bool)(await command.ExecuteScalarAsync() ?? false);
            return Success(exists, exists ? "Shop category exists" : "Shop category does not exist");
        }

        public async Task<InfrastructureResponse<bool>> ExistsBySlugAsync(int shopId, string slug, int? excludeId = null)
        {
            const string sql = @"
                SELECT EXISTS (
                    SELECT 1 FROM shop_categories
                    WHERE shop_id = @ShopId
                      AND slug = @Slug
                      AND (@ExcludeId::BIGINT IS NULL OR id <> @ExcludeId::BIGINT)
                );";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ShopId", (long)shopId);
            command.Parameters.AddWithValue("@Slug", slug);
            command.Parameters.AddWithValue("@ExcludeId", excludeId.HasValue ? (object)(long)excludeId.Value : DBNull.Value);

            var exists = (bool)(await command.ExecuteScalarAsync() ?? false);
            return Success(exists, exists ? "Slug already in use" : "Slug is available");
        }

        public async Task<InfrastructureResponse<bool>> HasChildCategoriesAsync(int shopId, int id)
        {
            const string sql = @"
                SELECT EXISTS (
                    SELECT 1 FROM shop_categories
                    WHERE shop_id = @ShopId AND parent_category_id = @Id
                );";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ShopId", (long)shopId);
            command.Parameters.AddWithValue("@Id", (long)id);

            var hasChildren = (bool)(await command.ExecuteScalarAsync() ?? false);
            return Success(hasChildren, hasChildren ? "Shop category has child categories" : "Shop category has no child categories");
        }

        public async Task<InfrastructureResponse<bool>> IsSelfOrDescendantAsync(int shopId, int ancestorId, int candidateId)
        {
            const string sql = @"
                WITH RECURSIVE subtree AS (
                    SELECT id FROM shop_categories
                    WHERE shop_id = @ShopId AND id = @AncestorId
                    UNION
                    SELECT c.id FROM shop_categories c
                    INNER JOIN subtree s ON c.parent_category_id = s.id
                    WHERE c.shop_id = @ShopId
                )
                SELECT EXISTS (SELECT 1 FROM subtree WHERE id = @CandidateId);";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ShopId", (long)shopId);
            command.Parameters.AddWithValue("@AncestorId", (long)ancestorId);
            command.Parameters.AddWithValue("@CandidateId", (long)candidateId);

            var result = (bool)(await command.ExecuteScalarAsync() ?? false);
            return Success(result, result ? "Candidate is within the category subtree" : "Candidate is outside the category subtree");
        }

        public async Task<InfrastructureResponse<List<Product>>> GetProductsByShopCategoryIdAsync(int shopId, int id)
        {
            const string sql = @"
                SELECT p.id, p.name, p.shop_id, p.global_category_id, p.shop_category_id, p.description,
                       p.stock_quantity, p.image_url, p.price, p.reserved_stock, p.low_stock_threshold,
                       p.is_active, p.sku, p.created_at, p.updated_at
                FROM products p
                WHERE p.shop_id = @ShopId AND p.shop_category_id = @Id
                ORDER BY p.id;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ShopId", (long)shopId);
            command.Parameters.AddWithValue("@Id", (long)id);

            await using var reader = await command.ExecuteReaderAsync();

            var products = new List<Product>();
            while (await reader.ReadAsync())
            {
                products.Add(MapProduct(reader));
            }

            return Success(products, "Products related to shop category retrieved successfully");
        }

        public async Task<InfrastructureResponse<int>> GetProductCountByShopCategoryIdAsync(int shopId, int id)
        {
            const string sql = "SELECT COUNT(*) FROM products WHERE shop_id = @ShopId AND shop_category_id = @Id;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ShopId", (long)shopId);
            command.Parameters.AddWithValue("@Id", (long)id);

            var count = Convert.ToInt32(await command.ExecuteScalarAsync());
            return Success(count, "Product count retrieved successfully");
        }

        #region Helpers

        private async Task<NpgsqlConnection> OpenConnectionAsync()
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        private static async Task<ShopCategory?> ReadSingleAsync(NpgsqlCommand command)
        {
            await using var reader = await command.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapShopCategory(reader) : null;
        }

        private static InfrastructureResponse<T> Success<T>(T value, string message) => new()
        {
            IsSuccess = true,
            Message = message,
            Value = value
        };

        private static bool TryMapConstraintViolation(PostgresException ex, out string marker)
        {
            marker = ex.SqlState switch
            {
                PostgresErrorCodes.UniqueViolation => ShopCategoryPersistenceErrors.DuplicateSlug,
                PostgresErrorCodes.ForeignKeyViolation => ShopCategoryPersistenceErrors.ForeignKeyViolation,
                _ => string.Empty
            };
            return marker.Length > 0;
        }

        private static void AddShopCategoryParameters(NpgsqlCommand command, ShopCategory category)
        {
            command.Parameters.AddWithValue("@ShopId", (long)category.ShopId);
            command.Parameters.AddWithValue("@Name", category.Name);
            command.Parameters.AddWithValue("@Description", (object?)category.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@ImageUrl", (object?)category.ImageUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("@Slug", category.Slug);
            command.Parameters.AddWithValue("@ParentCategoryId",
                category.ParentCategoryId.HasValue ? (object)(long)category.ParentCategoryId.Value : DBNull.Value);
            command.Parameters.AddWithValue("@IsActive", category.IsActive);
            command.Parameters.AddWithValue("@DisplayOrder", category.DisplayOrder);
        }

        private static ShopCategory MapShopCategory(NpgsqlDataReader reader)
        {
            var descriptionOrdinal = reader.GetOrdinal("description");
            var imageUrlOrdinal = reader.GetOrdinal("image_url");
            var parentOrdinal = reader.GetOrdinal("parent_category_id");

            return new ShopCategory
            {
                Id = checked((int)reader.GetInt64(reader.GetOrdinal("id"))),
                ShopId = checked((int)reader.GetInt64(reader.GetOrdinal("shop_id"))),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Description = reader.IsDBNull(descriptionOrdinal) ? null : reader.GetString(descriptionOrdinal),
                ImageUrl = reader.IsDBNull(imageUrlOrdinal) ? null : reader.GetString(imageUrlOrdinal),
                Slug = reader.GetString(reader.GetOrdinal("slug")),
                ParentCategoryId = reader.IsDBNull(parentOrdinal) ? null : checked((int)reader.GetInt64(parentOrdinal)),
                IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                DisplayOrder = reader.GetInt32(reader.GetOrdinal("display_order")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
            };
        }

        private static Product MapProduct(NpgsqlDataReader reader)
        {
            var globalCategoryOrdinal = reader.GetOrdinal("global_category_id");
            var shopCategoryOrdinal = reader.GetOrdinal("shop_category_id");

            return new Product
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                ShopId = checked((int)reader.GetInt64(reader.GetOrdinal("shop_id"))),
                GlobalCategoryId = reader.IsDBNull(globalCategoryOrdinal) ? null : checked((int)reader.GetInt64(globalCategoryOrdinal)),
                ShopCategoryId = reader.IsDBNull(shopCategoryOrdinal) ? null : checked((int)reader.GetInt64(shopCategoryOrdinal)),
                Description = reader.GetString(reader.GetOrdinal("description")),
                StockQuantity = reader.GetInt32(reader.GetOrdinal("stock_quantity")),
                ImageUrl = reader.GetString(reader.GetOrdinal("image_url")),
                Price = reader.GetDecimal(reader.GetOrdinal("price")),
                ReservedStock = reader.GetInt32(reader.GetOrdinal("reserved_stock")),
                LowStockThreshold = reader.GetInt32(reader.GetOrdinal("low_stock_threshold")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                SKU = reader.GetString(reader.GetOrdinal("sku")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
            };
        }

        #endregion
    }
}
