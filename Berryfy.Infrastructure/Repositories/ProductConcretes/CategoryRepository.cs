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
            command.Parameters.AddWithValue("@Id", category.Id);

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
            const string sql = "SELECT c.id, c.name, c.description, c.image_url, c.created_at, c.updated_at FROM Categories order by id";

            await OpenConnectionAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            var categories = new List<Category>();
            while (await reader.ReadAsync())
            {
                categories.Add(MapCategory(reader));
            }

            return categories;
        }

        public async Task<Category> GetByIdAsync(int id)
        {
            //using variables more good than select * --> using * is not too good
            const string sql = @"
                SELECT
                    c.id, c.name, c.description, c.image_url, c.created_at, c.updated_at
                WHERE c.id = @Id;";

            await OpenConnectionAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            await using var reader = await command.ExecuteReaderAsync();

            Category category = MapCategory(reader);

            return category;
        }

        public async Task<Category> GetByNameAsync(string name)
        {
            const string sql = @"
                SELECT
                    c.id, c.name, c.description, c.image_url, c.created_at, c.updated_at,
                FROM Categories
                WHERE c.name = @Name";

            await OpenConnectionAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Name", name);

            await using var reader = await command.ExecuteReaderAsync();

            Category category = MapCategory(reader);
            
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
            }
        }


        private async Task<NpgsqlConnection> OpenConnectionAsync()
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }
    }
}