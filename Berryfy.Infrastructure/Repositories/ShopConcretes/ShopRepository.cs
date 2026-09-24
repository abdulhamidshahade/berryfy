using Berryfy.Domain.Entities;
using Berryfy.Domain.Entities.ShopEntities;
using Berryfy.Domain.Repositories.ShopInterfaces;
using Berryfy.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Berryfy.Infrastructure.Repositories.ShopConcretes
{
    public class ShopRepository : IShopRepository
    {
        private readonly string _connectionString;

        public ShopRepository(IConfiguration config)
        {
            _connectionString = PostgresConnectionStrings.Resolve(config);
        }

        public async Task<InfrastructureResponse<Shop>> GetShopAsync(int id)
        {
            const string sql = @"
                SELECT
                    id, name, logo_url, description, email, phone, address, currency, language
                FROM Shops
                WHERE id = @id";

            await using var connection = await OpenConnectionAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("id", id);

            await using var reader = await command.ExecuteReaderAsync();

            Shop shop = null;

            if (await reader.ReadAsync())
            {
                shop = MapToShop(reader);
            }

            return new InfrastructureResponse<Shop>
            {
                IsSuccess = true,
                Message = "Shop data retrived successfully",
                Value = shop
            };
        }

        public async Task<InfrastructureResponse<Shop>> UpdateShopAsync(Shop shop)
        {
            const string sql = @"
                UPDATE Shops
                SET name = @Name,
                    logo_url = @LogoUrl,
                    description = @Description,
                    email = @Email,
                    phone = @Phone,
                    address = @Address,
                    currency = @Currency,
                    language = @Language
                WHERE id = @Id
                RETURNING id, name, logo_url, description, email, phone, address, currency, language";

            var connection = await OpenConnectionAsync();

            await using var command = new NpgsqlCommand(sql, connection);

            AddShopParameters(command, shop);
            
            command.Parameters.AddWithValue("Id", shop.Id);

            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                shop = MapToShop(reader);
            }

            return new InfrastructureResponse<Shop>()
            {
                IsSuccess = true,
                Message = "Shop updated successfully",
                Value = shop
            };
        }



        private async Task<NpgsqlConnection> OpenConnectionAsync()
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        private Shop MapToShop(NpgsqlDataReader reader)
        {
            return new Shop
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                LogoUrl = reader.GetString(reader.GetOrdinal("logo_url")),
                Description = reader.GetString(reader.GetOrdinal("description")),
                Email = reader.GetString(reader.GetOrdinal("email")),
                Phone = reader.GetString(reader.GetOrdinal("phone")),
                Address = reader.GetString(reader.GetOrdinal("address")),
                Currency = reader.GetString(reader.GetOrdinal("currency")),
                Language = reader.GetString(reader.GetOrdinal("language"))
            };
        }
        private void AddShopParameters(NpgsqlCommand command, Shop shop)
        {
            command.Parameters.AddWithValue("Name", (object?)shop.Name ?? DBNull.Value);
            command.Parameters.AddWithValue("LogoUrl", (object?)shop.LogoUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("Description", (object?)shop.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("Email", (object?)shop.Email ?? DBNull.Value);
            command.Parameters.AddWithValue("Phone", (object?)shop.Phone ?? DBNull.Value);
            command.Parameters.AddWithValue("Address", (object?)shop.Address ?? DBNull.Value);
            command.Parameters.AddWithValue("Currency", (object?)shop.Currency ?? DBNull.Value);
            command.Parameters.AddWithValue("Language", (object?)shop.Language ?? DBNull.Value);
        }
    }
}