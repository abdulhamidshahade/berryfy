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

        public async Task<InfrastructureResponse<GlobalCategory>> CreateAsync(GlobalCategory category)
        {
            const string sql = @"
                INSERT INTO global_categories (name, description, image_url, slug, parent_category_id, is_active, display_order, created_at, updated_at)
                VALUES (@Name, @Description, @ImageUrl, @Slug, @ParentCategoryId, @IsActive, @DisplayOrder, @CreatedAt, @UpdatedAt)
                RETURNING id, name, description, image_url, slug, parent_category_id, is_active, display_order, created_at, updated_at;";

            using var connection = await OpenConnectionAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            
            AddCategoryParameters(command, category);

            await using var reader = await command.ExecuteReaderAsync();

            GlobalCategory createdCategory = null;

            if (await reader.ReadAsync())
            {
                createdCategory = MapCategory(reader);
            }

            return new InfrastructureResponse<GlobalCategory>()
            {
                IsSuccess = true,
                Message = "Category created successfully",
                Value = createdCategory
            };
        }

        public async Task<InfrastructureResponse<bool>> DeleteAsync(GlobalCategory category)
        {
            const string sql = "DELETE FROM global_categories WHERE id = @Id";

            var connection = await OpenConnectionAsync();
            
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
            const string sql = "SELECT COUNT(1) FROM global_categories WHERE id = @Id";

            var connection = await OpenConnectionAsync();

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
            const string sql = "SELECT COUNT(1) FROM global_categories WHERE name = @Name";

            var connection = await OpenConnectionAsync();

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

        public async Task<InfrastructureResponse<IEnumerable<GlobalCategory>>> GetAllAsync()
        {
            const string sql = @"SELECT gc.id, gc.name, gc.description, gc.image_url, gc.slug, gc.parent_category_id,
                                gc.is_active, gc.display_order, gc.created_at, gc.updated_at
                                FROM global_categories gc order by gc.id";

            var connection = await OpenConnectionAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            var categories = new List<GlobalCategory>();
            while (await reader.ReadAsync())
            {
                categories.Add(MapCategory(reader));
            }

            return new InfrastructureResponse<IEnumerable<GlobalCategory>>()
            {
                IsSuccess = true,
                Message = "Categories retrieved successfully",
                Value = categories
            };
        }

        public async Task<InfrastructureResponse<GlobalCategory>> GetByIdAsync(int id)
        {
            //using variables more good than select * --> using * is not too good
            const string sql = @"
                SELECT
                    gc.id, gc.name, gc.description, gc.image_url, gc.slug, 
                    gc.parent_category_id, gc.is_active, gc.display_order, gc.created_at, gc.updated_at
                FROM global_categories gc
                WHERE gc.id = @Id;";

            using var connection = await OpenConnectionAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            await using var reader = await command.ExecuteReaderAsync();

            GlobalCategory category = null;

            if (await reader.ReadAsync())
            {
                category = MapCategory(reader);
            }

            return new InfrastructureResponse<GlobalCategory>()
            {
                IsSuccess = true,
                Message = "Category retrieved successfully",
                Value = category
            };
        }

        public async Task<InfrastructureResponse<GlobalCategory>> GetByNameAsync(string name)
        {
            const string sql = @"
                SELECT
                    gc.id, gc.name, gc.description, gc.image_url, gc.slug,
                    gc.parent_category_id, gc.is_active, gc.display_order, gc.created_at, gc.updated_at
                FROM global_categories gc
                WHERE gc.name = @Name";

            var connection = await OpenConnectionAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Name", name);

            await using var reader = await command.ExecuteReaderAsync();

            GlobalCategory category = null;

            if (await reader.ReadAsync())
            {
                category = MapCategory(reader);
            }

            
            
            return new InfrastructureResponse<GlobalCategory>()
            {
                IsSuccess = true,
                Message = "Category retrieved successfully",
                Value = category
            };
        } 

        public async Task<InfrastructureResponse<GlobalCategory>> UpdateAsync(int id, GlobalCategory category)
        {
            const string sql = @"
                UPDATE global_categories
                SET name = @Name,
                    description = @Description,
                    image_url = @ImageUrl,
                    slug = @Slug,
                    parent_category_id = @ParentCategoryId,
                    is_active = @IsActive,
                    display_order = @DisplayOrder,
                    updated_at = @UpdatedAt
                WHERE id = @Id
                RETURNING id, name, description, image_url, slug, parent_category_id, is_active, display_order, created_at, updated_at;";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            
            AddCategoryParameters(command, category);
            command.Parameters.AddWithValue("@Id", id);

            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                category = MapCategory(reader);
            }
            
            return new InfrastructureResponse<GlobalCategory>()
            {
                IsSuccess = true,
                Message = "Category updated successfully",
                Value = category
            };
        }

        private GlobalCategory MapCategory(NpgsqlDataReader reader)
        {
            return new GlobalCategory()
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Description = reader.GetString(reader.GetOrdinal("description")),
                ImageUrl = reader.GetString(reader.GetOrdinal("image_url")),
                Slug = reader.GetString(reader.GetOrdinal("slug")),
                ParentCategoryId = reader.IsDBNull(reader.GetOrdinal("parent_category_id")) ? null : reader.GetInt32(reader.GetOrdinal("parent_category_id")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                DisplayOrder = reader.GetInt32(reader.GetOrdinal("display_order")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
            };
        }

        //19:46

        private async Task<NpgsqlConnection> OpenConnectionAsync()
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        private void AddCategoryParameters(NpgsqlCommand command, GlobalCategory category)
        {
            command.Parameters.AddWithValue("@Name", category.Name);
            command.Parameters.AddWithValue("@Description", category.Description);
            command.Parameters.AddWithValue("@ImageUrl", category.ImageUrl);
            command.Parameters.AddWithValue("@Slug", category.Slug);
            command.Parameters.AddWithValue(
                                            "@ParentCategoryId",
                                                (object?)category.ParentCategoryId ?? DBNull.Value
);
            command.Parameters.AddWithValue("@IsActive", category.IsActive);
            command.Parameters.AddWithValue("@DisplayOrder", category.DisplayOrder);
            command.Parameters.AddWithValue("@CreatedAt", category.CreatedAt);
            command.Parameters.AddWithValue("@UpdatedAt", category.UpdatedAt);
        }

        public async Task<InfrastructureResponse<List<Product>>> GetProductsByGlobalCategoryId(int globalCategoryId)
        {
            string query = "Select * from products p where p.global_category_id = @GlobalCategoryId";

            using var connection = await OpenConnectionAsync();

            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@GlobalCategoryId", globalCategoryId);

            List<Product> products = new List<Product>();

            using var reader = await command.ExecuteReaderAsync();

            while(await reader.ReadAsync())
            {
                products.Add(MapProduct(reader, new Product()));
            }

            return new InfrastructureResponse<List<Product>>()
            {
                IsSuccess = true,
                Message = "Products that related to category retrived successfully.",
                Value = products
            };
        }


        private Product MapProduct(NpgsqlDataReader reader, Product product)
        {
            product = new Product()
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
                ShopId = reader.GetInt32(reader.GetOrdinal("shop_id")),
                GlobalCategoryId = reader.IsDBNull(reader.GetOrdinal("global_category_id")) ? null : reader.GetInt32(reader.GetOrdinal("global_category_id")),
                ShopCategoryId = reader.IsDBNull(reader.GetOrdinal("shop_category_id")) ? null : reader.GetInt32(reader.GetOrdinal("shop_category_id"))
            };
            return product;
        }

        public async Task<InfrastructureResponse<int>> GetProductCountByGlobalCategoryIdAsync(int globalCategoryId)
        {
            string query = "select count(*) from products where global_category_id = @GlobalCategoryId;";

            using var connection = await OpenConnectionAsync();

            using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@GlobalCategoryId", globalCategoryId);

            var result = await command.ExecuteScalarAsync();

            return new InfrastructureResponse<int>()
            {
                IsSuccess = true,
                Value = Convert.ToInt32(result)
            };
        }
    }
}