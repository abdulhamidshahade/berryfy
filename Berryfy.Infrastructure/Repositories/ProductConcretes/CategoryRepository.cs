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

        public async Task<Category> CreateAsync(Category category)
        {
            const string sql = @"
                INSERT INTO Categories (Name, Description, ImageUrl, CreatedAt, UpdatedAt)
                VALUES (@Name, @Description, @ImageUrl, @CreatedAt, @UpdatedAt)
                RETURNING Id, CreatedAt, UpdatedAt;";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("Name", category.Name);
            command.Parameters.AddWithValue("Description", (object?)category.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("ImageUrl", (object?)category.ImageUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("CreatedAt", category.CreatedAt);
            command.Parameters.AddWithValue("UpdatedAt", category.UpdatedAt);

            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                category.Id = reader.GetInt32(reader.GetOrdinal("Id"));
                category.CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"));
                category.UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"));
            }

            return category;
        }

        public async Task<bool> DeleteAsync(Category category)
        {
            const string sql = "DELETE FROM Categories WHERE Id = @Id";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("Id", category.Id);

            var affected = await command.ExecuteNonQueryAsync();
            return affected > 0;
        }

        public async Task<bool> ExistsByIdAsync(int id)
        {
            const string sql = "SELECT COUNT(1) FROM Categories WHERE Id = @Id";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("Id", id);

            var count = await command.ExecuteScalarAsync();
            return count != null && Convert.ToInt32(count) > 0;
        }

        public async Task<bool> ExistsByNameAsync(string name)
        {
            const string sql = "SELECT COUNT(1) FROM Categories WHERE Name = @Name";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("Name", name);

            var count = await command.ExecuteScalarAsync();
            return count != null && Convert.ToInt32(count) > 0;
        }

        public async Task<IEnumerable<Category>> GetAllAsync()
        {
            const string sql = "SELECT Id, Name, Description, ImageUrl, CreatedAt, UpdatedAt FROM Categories";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            var categories = new List<Category>();
            while (await reader.ReadAsync())
            {
                categories.Add(new Category
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Description = reader.GetString(reader.GetOrdinal("Description")),
                    ImageUrl = reader.GetString(reader.GetOrdinal("ImageUrl")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                    ProductCategories = new List<ProductCategory>()
                });
            }

            return categories;
        }

        public async Task<Category> GetByIdAsync(int id)
        {
            const string sql = @"
                SELECT
                    c.Id, c.Name, c.Description, c.ImageUrl, c.CreatedAt, c.UpdatedAt,
                    pc.Id AS PcId, pc.ProductId, pc.CreatedAt AS PcCreatedAt, pc.UpdatedAt AS PcUpdatedAt,
                    p.Id AS P_Id, p.Name AS P_Name, p.Description AS P_Description, p.StockQuantity,
                    p.ImageUrl AS P_ImageUrl, p.Price, p.ReservedStock, p.LowStockThreshold,
                    p.IsActive, p.SKU, p.CreatedAt AS P_CreatedAt, p.UpdatedAt AS P_UpdatedAt
                FROM Categories c
                LEFT JOIN ProductCategories pc ON c.Id = pc.CategoryId
                LEFT JOIN Products p ON pc.ProductId = p.Id
                WHERE c.Id = @Id";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("Id", id);

            await using var reader = await command.ExecuteReaderAsync();

            Category? category = null;
            var productCategories = new Dictionary<int, ProductCategory>();

            while (await reader.ReadAsync())
            {
                if (category == null)
                {
                    category = new Category
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        Description = reader.GetString(reader.GetOrdinal("Description")),
                        ImageUrl = reader.GetString(reader.GetOrdinal("ImageUrl")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                        ProductCategories = new List<ProductCategory>()
                    };
                }

                if (!reader.IsDBNull(reader.GetOrdinal("PcId")))
                {
                    var pcId = reader.GetInt32(reader.GetOrdinal("PcId"));
                    if (!productCategories.TryGetValue(pcId, out var pc))
                    {
                        pc = new ProductCategory
                        {
                            Id = pcId,
                            ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("PcCreatedAt")),
                            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("PcUpdatedAt"))
                        };
                        productCategories.Add(pcId, pc);
                        category.ProductCategories.Add(pc);
                    }

                    if (!reader.IsDBNull(reader.GetOrdinal("P_Id")))
                    {
                        if (pc.Product == null)
                        {
                            pc.Product = new Product
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("P_Id")),
                                Name = reader.GetString(reader.GetOrdinal("P_Name")),
                                Description = reader.GetString(reader.GetOrdinal("P_Description")),
                                StockQuantity = reader.GetInt32(reader.GetOrdinal("StockQuantity")),
                                ImageUrl = reader.GetString(reader.GetOrdinal("P_ImageUrl")),
                                Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                                ReservedStock = reader.GetInt32(reader.GetOrdinal("ReservedStock")),
                                LowStockThreshold = reader.GetInt32(reader.GetOrdinal("LowStockThreshold")),
                                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                                SKU = reader.GetString(reader.GetOrdinal("SKU")),
                                CreatedAt = reader.GetDateTime(reader.GetOrdinal("P_CreatedAt")),
                                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("P_UpdatedAt"))
                            };
                        }
                    }
                }
            }

            return category;
        }

        public async Task<Category> GetByNameAsync(string name)
        {
            const string sql = @"
                SELECT
                    c.Id, c.Name, c.Description, c.ImageUrl, c.CreatedAt, c.UpdatedAt,
                    pc.Id AS PcId, pc.ProductId, pc.CreatedAt AS PcCreatedAt, pc.UpdatedAt AS PcUpdatedAt,
                    p.Id AS P_Id, p.Name AS P_Name, p.Description AS P_Description, p.StockQuantity,
                    p.ImageUrl AS P_ImageUrl, p.Price, p.ReservedStock, p.LowStockThreshold,
                    p.IsActive, p.SKU, p.CreatedAt AS P_CreatedAt, p.UpdatedAt AS P_UpdatedAt
                FROM Categories c
                LEFT JOIN ProductCategories pc ON c.Id = pc.CategoryId
                LEFT JOIN Products p ON pc.ProductId = p.Id
                WHERE c.Name = @Name";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("Name", name);

            await using var reader = await command.ExecuteReaderAsync();

            Category? category = null;
            var productCategories = new Dictionary<int, ProductCategory>();

            while (await reader.ReadAsync())
            {
                if (category == null)
                {
                    category = new Category
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        Description = reader.GetString(reader.GetOrdinal("Description")),
                        ImageUrl = reader.GetString(reader.GetOrdinal("ImageUrl")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                        ProductCategories = new List<ProductCategory>()
                    };
                }

                if (!reader.IsDBNull(reader.GetOrdinal("PcId")))
                {
                    var pcId = reader.GetInt32(reader.GetOrdinal("PcId"));
                    if (!productCategories.TryGetValue(pcId, out var pc))
                    {
                        pc = new ProductCategory
                        {
                            Id = pcId,
                            ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("PcCreatedAt")),
                            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("PcUpdatedAt"))
                        };
                        productCategories.Add(pcId, pc);
                        category.ProductCategories.Add(pc);
                    }

                    if (!reader.IsDBNull(reader.GetOrdinal("P_Id")))
                    {
                        if (pc.Product == null)
                        {
                            pc.Product = new Product
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("P_Id")),
                                Name = reader.GetString(reader.GetOrdinal("P_Name")),
                                Description = reader.GetString(reader.GetOrdinal("P_Description")),
                                StockQuantity = reader.GetInt32(reader.GetOrdinal("StockQuantity")),
                                ImageUrl = reader.GetString(reader.GetOrdinal("P_ImageUrl")),
                                Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                                ReservedStock = reader.GetInt32(reader.GetOrdinal("ReservedStock")),
                                LowStockThreshold = reader.GetInt32(reader.GetOrdinal("LowStockThreshold")),
                                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                                SKU = reader.GetString(reader.GetOrdinal("SKU")),
                                CreatedAt = reader.GetDateTime(reader.GetOrdinal("P_CreatedAt")),
                                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("P_UpdatedAt"))
                            };
                        }
                    }
                }
            }

            return category;
        }

        public async Task<Category> UpdateAsync(int id, Category category)
        {
            const string sql = @"
                UPDATE Categories
                SET Name = @Name,
                    Description = @Description,
                    ImageUrl = @ImageUrl,
                    UpdatedAt = @UpdatedAt
                WHERE Id = @Id
                RETURNING Id, Name, Description, ImageUrl, CreatedAt, UpdatedAt;";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("Name", category.Name);
            command.Parameters.AddWithValue("Description", (object?)category.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("ImageUrl", (object?)category.ImageUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("UpdatedAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("Id", id);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                category.Id = reader.GetInt32(reader.GetOrdinal("Id"));
                category.Name = reader.GetString(reader.GetOrdinal("Name"));
                category.Description = reader.GetString(reader.GetOrdinal("Description"));
                category.ImageUrl = reader.GetString(reader.GetOrdinal("ImageUrl"));
                category.CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"));
                category.UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"));
                return category;
            }

            return null;
        }
    }
}