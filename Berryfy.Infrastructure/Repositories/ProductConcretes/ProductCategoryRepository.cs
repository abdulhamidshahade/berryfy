using Berryfy.Domain.Entities.ProductEntities;
using Berryfy.Domain.Repositories.ProductInterfaces;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Berryfy.Infrastructure.Repositories.ProductConcretes
{
    public class ProductCategoryRepository : IProductCategoryRepository
    {
        private readonly string _connectionString;

        public ProductCategoryRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("PostgreSQLServer");
        }

        public async Task<bool> AddProductCategoryAsync(Product product, List<int> categories)
        {
            if (categories == null || categories.Count == 0)
                return false;

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                const string sql = @"
                    INSERT INTO ProductCategories (ProductId, CategoryId, CreatedAt, UpdatedAt)
                    VALUES (@ProductId, @CategoryId, @CreatedAt, @UpdatedAt);";

                var now = DateTime.UtcNow;

                foreach (var categoryId in categories)
                {
                    await using var command = new NpgsqlCommand(sql, connection, transaction);
                    command.Parameters.AddWithValue("ProductId", product.Id);
                    command.Parameters.AddWithValue("CategoryId", categoryId);
                    command.Parameters.AddWithValue("CreatedAt", now);
                    command.Parameters.AddWithValue("UpdatedAt", now);

                    await command.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> UpdateProductCategoryAsync(Product product, List<int> categories)
        {
            return await AddProductCategoryAsync(product, categories);
        }

        public async Task<List<Category>> GetCategoriesByProuductId(int productId)
        {
            const string sql = @"
                SELECT c.Id, c.Name, c.Description, c.ImageUrl, c.CreatedAt, c.UpdatedAt
                FROM Categories c
                INNER JOIN ProductCategories pc ON c.Id = pc.CategoryId
                WHERE pc.ProductId = @ProductId
                ORDER BY c.Id;";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("ProductId", productId);

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
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
                });
            }

            return categories;
        }

        public async Task<bool> RemoveCategoriesByProductId(int productId)
        {
            const string sql = "DELETE FROM ProductCategories WHERE ProductId = @ProductId";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("ProductId", productId);

            var affected = await command.ExecuteNonQueryAsync();
            return affected > 0;
        }
    }
}