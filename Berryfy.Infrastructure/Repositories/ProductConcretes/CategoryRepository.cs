using AutoMapper.Configuration.Annotations;
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
                INSERT INTO Categories (name, description, image_url, created_at, updated_at)
                VALUES (@Name, @Description, @ImageUrl, @CreatedAt, @UpdatedAt)
                RETURNING id, name, description, image_url, created_at, updated_at;";

            using var connection = await OpenConnectionAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            
            AddCategoryParameters(command, category);

            await using var reader = await command.ExecuteReaderAsync();

            Category createdCategory = null;

            if (await reader.ReadAsync())
            {
                createdCategory = MapCategory(reader);
            }

            return new InfrastructureResponse<Category>()
            {
                IsSuccess = true,
                Message = "Category created successfully",
                Value = createdCategory
            };
        }

        public async Task<InfrastructureResponse<bool>> DeleteAsync(Category category)
        {
            const string sql = "DELETE FROM Categories WHERE id = @Id";

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
            const string sql = "SELECT COUNT(1) FROM Categories WHERE id = @Id";

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
            const string sql = "SELECT COUNT(1) FROM Categories WHERE name = @Name";

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

        public async Task<InfrastructureResponse<IEnumerable<Category>>> GetAllAsync()
        {
            const string sql = @"SELECT c.id, c.name, c.description, c.image_url, c.created_at, c.updated_at
                                FROM Categories c order by c.id";

            var connection = await OpenConnectionAsync();

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
            //using variables more good than select * --> using * is not too good
            const string sql = @"
                SELECT
                    c.id, c.name, c.description, c.image_url, c.created_at, c.updated_at
                FROM categories c
                WHERE c.id = @Id;";

            using var connection = await OpenConnectionAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            await using var reader = await command.ExecuteReaderAsync();

            Category category = null;

            if (await reader.ReadAsync())
            {
                category = MapCategory(reader);
            }

            return new InfrastructureResponse<Category>()
            {
                IsSuccess = true,
                Message = "Category retrieved successfully",
                Value = category
            };
        }

        public async Task<InfrastructureResponse<Category>> GetByNameAsync(string name)
        {
            const string sql = @"
                SELECT
                    c.id, c.name, c.description, c.image_url, c.created_at, c.updated_at
                FROM categories c
                WHERE c.name = @Name";

            var connection = await OpenConnectionAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Name", name);

            await using var reader = await command.ExecuteReaderAsync();

            Category category = null;

            if (await reader.ReadAsync())
            {
                category = MapCategory(reader);
            }

            
            
            return new InfrastructureResponse<Category>()
            {
                IsSuccess = true,
                Message = "Category retrieved successfully",
                Value = category
            };
        } 

        public async Task<InfrastructureResponse<Category>> UpdateAsync(int id, Category category)
        {
            const string sql = @"
                UPDATE categories
                SET name = @Name,
                    description = @Description,
                    image_url = @ImageUrl,
                    updated_at = @UpdatedAt
                WHERE id = @Id
                RETURNING id, name, description, image_url, created_at, updated_at;";

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
            
            return new InfrastructureResponse<Category>()
            {
                IsSuccess = true,
                Message = "Category updated successfully",
                Value = category
            };
        }

        private Category MapCategory(NpgsqlDataReader reader)
        {
            return new Category()
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Description = reader.GetString(reader.GetOrdinal("description")),
                ImageUrl = reader.GetString(reader.GetOrdinal("image_url")),
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

        private void AddCategoryParameters(NpgsqlCommand command, Category category)
        {
            command.Parameters.AddWithValue("@Name", category.Name);
            command.Parameters.AddWithValue("@Description", category.Description);
            command.Parameters.AddWithValue("@ImageUrl", category.ImageUrl);
            command.Parameters.AddWithValue("@CreatedAt", category.CreatedAt);
            command.Parameters.AddWithValue("@UpdatedAt", category.UpdatedAt);
        }

        public async Task<InfrastructureResponse<List<Product>>> GetProductsByCategoryId(int categoryId)
        {
            string query = "Select p.* from products p inner join product_categories pc on (p.id = pc.product_id) where pc.category_id = @CategoryId";

            using var connection = await OpenConnectionAsync();

            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@CategoryId", categoryId);

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
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
            };
            return product;
        }

        public async Task<InfrastructureResponse<int>> GetProductCountByCategoryIdAsync(int categoryId)
        {
            string query = "select count(*) from product_categories where category_id = @CategoryId;";

            using var connection = await OpenConnectionAsync();

            using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@CategoryId", categoryId);

            var result = await command.ExecuteScalarAsync();

            return new InfrastructureResponse<int>()
            {
                IsSuccess = true,
                Value = Convert.ToInt32(result)
            };
        }
    }
}