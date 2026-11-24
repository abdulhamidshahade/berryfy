using Berryfy.Domain.Entities;
using Berryfy.Domain.Entities.ProductEntities;
using Berryfy.Domain.Repositories.ProductInterfaces;
using Berryfy.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Berryfy.Infrastructure.Repositories.ProductConcretes
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly string _connectionString;

        public CategoryRepository(IConfiguration config)
        {
            _connectionString = PostgresConnectionStrings.Resolve(config);
        }

        public async Task<InfrastructureResponse<Category>> CreateAsync(Category category)
        {
            const string sql = @"
                INSERT INTO categories (name, description, image_url, is_active, created_at, updated_at)
                VALUES (@Name, @Description, @ImageUrl, @IsActive, @CreatedAt, @UpdatedAt)
                RETURNING id, name, description, image_url, is_active, created_at, updated_at;";

            var now = DateTime.UtcNow;
            if (category.CreatedAt == default) category.CreatedAt = now;
            if (category.UpdatedAt == default) category.UpdatedAt = now;

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);

            AddCategoryParameters(command, category);

            await using var reader = await command.ExecuteReaderAsync();

            Category? createdCategory = null;

            if (await reader.ReadAsync())
            {
                createdCategory = MapCategory(reader);
            }

            return new InfrastructureResponse<Category>()
            {
                IsSuccess = createdCategory != null,
                Message = createdCategory != null ? "Category created successfully" : "Failed to create category",
                Value = createdCategory!
            };
        }

        public async Task<InfrastructureResponse<bool>> DeleteAsync(Category category)
        {
            const string sql = "DELETE FROM categories WHERE id = @Id";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", category.Id);

            var rowAffected = await command.ExecuteNonQueryAsync();
            return new InfrastructureResponse<bool>()
            {
                IsSuccess = true,
                Message = "Category deleted successfully",
                Value = rowAffected > 0
            };
        }

        public async Task<InfrastructureResponse<bool>> ExistsByIdAsync(int id)
        {
            const string sql = "SELECT COUNT(1) FROM categories WHERE id = @Id";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            var count = await command.ExecuteScalarAsync();
            return new InfrastructureResponse<bool>()
            {
                IsSuccess = true,
                Message = "Category exists",
                Value = count != null && Convert.ToInt32(count) > 0
            };
        }

        public async Task<InfrastructureResponse<bool>> ExistsByNameAsync(string name)
        {
            const string sql = "SELECT COUNT(1) FROM categories WHERE LOWER(name) = LOWER(@Name)";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Name", name);

            var count = await command.ExecuteScalarAsync();
            return new InfrastructureResponse<bool>()
            {
                IsSuccess = true,
                Message = "Category exists",
                Value = count != null && Convert.ToInt32(count) > 0
            };
        }

        public async Task<InfrastructureResponse<IEnumerable<Category>>> GetAllAsync()
        {
            const string sql = @"SELECT c.id, c.name, c.description, c.image_url,
                                c.is_active, c.created_at, c.updated_at
                                FROM categories c ORDER BY c.id";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            var categories = new List<Category>();
            while (await reader.ReadAsync())
            {
                categories.Add(MapCategory(reader));
            }

            return new InfrastructureResponse<IEnumerable<Category>>()
            {
                IsSuccess = true,
                Message = "Categories retrieved successfully",
                Value = categories
            };
        }

        public async Task<InfrastructureResponse<Category>> GetByIdAsync(int id)
        {
            const string sql = @"
                SELECT
                    c.id, c.name, c.description, c.image_url,
                    c.is_active, c.created_at, c.updated_at
                FROM categories c
                WHERE c.id = @Id;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            await using var reader = await command.ExecuteReaderAsync();

            Category? category = null;

            if (await reader.ReadAsync())
            {
                category = MapCategory(reader);
            }

            return new InfrastructureResponse<Category>()
            {
                IsSuccess = category != null,
                Message = category != null ? "Category retrieved successfully" : "Category not found",
                Value = category!
            };
        }

        public async Task<InfrastructureResponse<Category>> GetByNameAsync(string name)
        {
            const string sql = @"
                SELECT
                    c.id, c.name, c.description, c.image_url,
                    c.is_active, c.created_at, c.updated_at
                FROM categories c
                WHERE LOWER(c.name) = LOWER(@Name)";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Name", name);

            await using var reader = await command.ExecuteReaderAsync();

            Category? category = null;

            if (await reader.ReadAsync())
            {
                category = MapCategory(reader);
            }

            return new InfrastructureResponse<Category>()
            {
                IsSuccess = category != null,
                Message = category != null ? "Category retrieved successfully" : "Category not found",
                Value = category!
            };
        }

        public async Task<InfrastructureResponse<Category>> UpdateAsync(int id, Category category)
        {
            const string sql = @"
                UPDATE categories
                SET name = @Name,
                    description = @Description,
                    image_url = @ImageUrl,
                    is_active = @IsActive,
                    updated_at = @UpdatedAt
                WHERE id = @Id
                RETURNING id, name, description, image_url, is_active, created_at, updated_at;";

            if (category.UpdatedAt == default) category.UpdatedAt = DateTime.UtcNow;

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);

            AddCategoryParameters(command, category);
            command.Parameters.AddWithValue("@Id", id);

            await using var reader = await command.ExecuteReaderAsync();

            Category? updatedCategory = null;
            if (await reader.ReadAsync())
            {
                updatedCategory = MapCategory(reader);
            }

            return new InfrastructureResponse<Category>()
            {
                IsSuccess = updatedCategory != null,
                Message = updatedCategory != null ? "Category updated successfully" : "Category not found",
                Value = updatedCategory!
            };
        }

        private Category MapCategory(NpgsqlDataReader reader)
        {
            var descOrd = reader.GetOrdinal("description");
            var imgOrd = reader.GetOrdinal("image_url");
            return new Category()
            {
                Id = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("id"))),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Description = reader.IsDBNull(descOrd) ? string.Empty : reader.GetString(descOrd),
                ImageUrl = reader.IsDBNull(imgOrd) ? string.Empty : reader.GetString(imgOrd),
                IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
            };
        }

        private async Task<NpgsqlConnection> OpenConnectionAsync()
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        private void AddCategoryParameters(NpgsqlCommand command, Category category)
        {
            command.Parameters.AddWithValue("@Name", category.Name ?? string.Empty);
            command.Parameters.AddWithValue("@Description", category.Description ?? string.Empty);
            command.Parameters.AddWithValue("@ImageUrl", (object?)category.ImageUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("@IsActive", category.IsActive);
            command.Parameters.AddWithValue("@CreatedAt", category.CreatedAt == default ? DateTime.UtcNow : category.CreatedAt);
            command.Parameters.AddWithValue("@UpdatedAt", category.UpdatedAt == default ? DateTime.UtcNow : category.UpdatedAt);
        }

        public async Task<InfrastructureResponse<List<Product>>> GetProductsByGlobalCategoryId(int globalCategoryId)
        {
            const string query = "SELECT * FROM products p WHERE p.category_id = @CategoryId ORDER BY p.id";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@CategoryId", globalCategoryId);

            List<Product> products = new List<Product>();

            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                products.Add(MapProduct(reader));
            }

            return new InfrastructureResponse<List<Product>>()
            {
                IsSuccess = true,
                Message = "Products related to category retrieved successfully.",
                Value = products
            };
        }

        private Product MapProduct(NpgsqlDataReader reader)
        {
            var descOrd = reader.GetOrdinal("description");
            var imgOrd = reader.GetOrdinal("image_url");
            var skuOrd = reader.GetOrdinal("sku");
            var catOrd = reader.GetOrdinal("category_id");
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
                CategoryId = reader.IsDBNull(catOrd) ? 0 : Convert.ToInt32(reader.GetValue(catOrd))
            };
        }

        public async Task<InfrastructureResponse<int>> GetProductCountByGlobalCategoryIdAsync(int globalCategoryId)
        {
            const string query = "SELECT COUNT(*) FROM products WHERE category_id = @CategoryId;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@CategoryId", globalCategoryId);

            var result = await command.ExecuteScalarAsync();

            return new InfrastructureResponse<int>()
            {
                IsSuccess = true,
                Value = Convert.ToInt32(result)
            };
        }
    }
}