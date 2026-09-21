using Berryfy.Domain.Entities.ShopEntities;
using Berryfy.Domain.Repositories.ShopInterfaces;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Berryfy.Infrastructure.Repositories.ShopConcretes
{
    public class ShopRepository : IShopRepository
    {
        private readonly string _connectionString;

        public ShopRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("PostgreSQLServer");
        }

        public async Task<Shop> GetShopAsync(int id)
        {
            const string sql = @"
                SELECT
                    Id, Name, LogoUrl, Description, Email, Phone, Address, Currency, Language
                FROM Shops
                WHERE Id = @Id";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("Id", id);

            await using var reader = await command.ExecuteReaderAsync();

            Shop shop = null;

            if (await reader.ReadAsync())
            {
                shop = new Shop
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    LogoUrl = reader.GetString(reader.GetOrdinal("LogoUrl")),
                    Description = reader.GetString(reader.GetOrdinal("Description")),
                    Email = reader.GetString(reader.GetOrdinal("Email")),
                    Phone = reader.GetString(reader.GetOrdinal("Phone")),
                    Address = reader.GetString(reader.GetOrdinal("Address")),
                    Currency = reader.GetString(reader.GetOrdinal("Currency")),
                    Language = reader.GetString(reader.GetOrdinal("Language"))
                };
            }

            return shop;
        }

        public async Task<Shop> UpdateShopAsync(Shop shop)
        {
            const string sql = @"
                UPDATE Shops
                SET Name = @Name,
                    LogoUrl = @LogoUrl,
                    Description = @Description,
                    Email = @Email,
                    Phone = @Phone,
                    Address = @Address,
                    Currency = @Currency,
                    Language = @Language
                WHERE Id = @Id
                RETURNING Id, Name, LogoUrl, Description, Email, Phone, Address, Currency, Language";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("Name", (object?)shop.Name ?? DBNull.Value);
            command.Parameters.AddWithValue("LogoUrl", (object?)shop.LogoUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("Description", (object?)shop.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("Email", (object?)shop.Email ?? DBNull.Value);
            command.Parameters.AddWithValue("Phone", (object?)shop.Phone ?? DBNull.Value);
            command.Parameters.AddWithValue("Address", (object?)shop.Address ?? DBNull.Value);
            command.Parameters.AddWithValue("Currency", (object?)shop.Currency ?? DBNull.Value);
            command.Parameters.AddWithValue("Language", (object?)shop.Language ?? DBNull.Value);
            command.Parameters.AddWithValue("Id", shop.Id);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                shop.Id = reader.GetInt32(reader.GetOrdinal("Id"));
                shop.Name = reader.GetString(reader.GetOrdinal("Name"));
                shop.LogoUrl = reader.GetString(reader.GetOrdinal("LogoUrl"));
                shop.Description = reader.GetString(reader.GetOrdinal("Description"));
                shop.Email = reader.GetString(reader.GetOrdinal("Email"));
                shop.Phone = reader.GetString(reader.GetOrdinal("Phone"));
                shop.Address = reader.GetString(reader.GetOrdinal("Address"));
                shop.Currency = reader.GetString(reader.GetOrdinal("Currency"));
                shop.Language = reader.GetString(reader.GetOrdinal("Language"));
                return shop;
            }

            return null;
        }
    }
}