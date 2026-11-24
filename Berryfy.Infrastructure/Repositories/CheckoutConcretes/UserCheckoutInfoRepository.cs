using Berryfy.Domain.Entities.CheckoutEntities;
using Berryfy.Domain.Repositories.CheckoutInterfaces;
using Microsoft.EntityFrameworkCore;
using Berryfy.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Berryfy.Domain.Entities;

namespace Berryfy.Infrastructure.Repositories.CheckoutConcretes
{
    public class UserCheckoutInfoRepository : IUserCheckoutInfoRepository
    {
        private readonly string _connectionString;

        public UserCheckoutInfoRepository(IConfiguration config)
        {
            _connectionString = PostgresConnectionStrings.Resolve(config);
        }

        public async Task<InfrastructureResponse<UserCheckoutInfo?>> GetByUserIdAsync(int userId)
        {
            const string query = @"SELECT * FROM user_checkout_infos WHERE user_id = @UserId ORDER BY id DESC LIMIT 1";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return new InfrastructureResponse<UserCheckoutInfo?>()
                {
                    IsSuccess = false,
                    Value = null
                };
            }

            var checkoutInfo = MapUserCheckoutInfo(reader);

            return new InfrastructureResponse<UserCheckoutInfo?>()
            {
                IsSuccess = true,
                Message = "The process completed successfully",
                Value = checkoutInfo
            };
        }

        public async Task<InfrastructureResponse<UserCheckoutInfo?>> GetBySessionIdAsync(string sessionId)
        {
            const string query = @"SELECT * FROM user_checkout_infos WHERE session_id = @sessionId ORDER BY id DESC LIMIT 1";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@sessionId", sessionId);

            UserCheckoutInfo? checkoutInfo = null;
            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                checkoutInfo = MapUserCheckoutInfo(reader);
            }

            return new InfrastructureResponse<UserCheckoutInfo?>()
            {
                IsSuccess = checkoutInfo != null,
                Message = checkoutInfo != null ? "The process completed successfully" : "Checkout info not found",
                Value = checkoutInfo
            };
        }

        public async Task<InfrastructureResponse<UserCheckoutInfo>> CreateAsync(UserCheckoutInfo checkoutInfo)
        {
            const string query = @"
                INSERT INTO user_checkout_infos (
                    user_id, session_id, firstname, lastname, email,
                    phone, payer_email, billing_city, billing_state, billing_country, updated_at, address, address2, city, state, zipcode,
                    country, payer_name, billing_address1, billing_address2, billing_postal_code, created_at, last_used_at)
                VALUES (
                    @UserId, @SessionId, @FirstName, @LastName, @Email,
                    @Phone, @PayerEmail, @BillingCity, @BillingState, @BillingCountry,
                    @UpdatedAt, @Address, @Address2, @City, @State, @ZipCode, @Country, @PayerName, @BillingAddress1, @BillingAddress2,
                    @BillingPostalCode, @CreatedAt, @LastUsedAt)
                RETURNING *;";

            var now = DateTime.UtcNow;
            if (checkoutInfo.CreatedAt == default) checkoutInfo.CreatedAt = now;
            if (checkoutInfo.UpdatedAt == default) checkoutInfo.UpdatedAt = now;

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(query, connection);

            AddUserCheckoutInfoParameters(command, checkoutInfo);

            await using var reader = await command.ExecuteReaderAsync();
            UserCheckoutInfo? checkoutInfoObj = null;

            if (await reader.ReadAsync())
            {
                checkoutInfoObj = MapUserCheckoutInfo(reader);
            }

            return new InfrastructureResponse<UserCheckoutInfo>()
            {
                IsSuccess = checkoutInfoObj != null,
                Message = "The process completed successfully",
                Value = checkoutInfoObj!
            };
        }

        public async Task<InfrastructureResponse<bool>> UpdateAsync(UserCheckoutInfo checkoutInfo)
        {
            const string query = @"
                UPDATE user_checkout_infos
                SET user_id = @UserId,
                    session_id = @SessionId,
                    firstname = @FirstName,
                    lastname = @LastName,
                    email = @Email,
                    phone = @Phone,
                    payer_email = @PayerEmail,
                    billing_city = @BillingCity,
                    billing_state = @BillingState,
                    billing_country = @BillingCountry,
                    updated_at = @UpdatedAt,
                    address = @Address,
                    address2 = @Address2,
                    city = @City,
                    state = @State,
                    zipcode = @ZipCode,
                    country = @Country,
                    payer_name = @PayerName,
                    billing_address1 = @BillingAddress1,
                    billing_address2 = @BillingAddress2,
                    billing_postal_code = @BillingPostalCode,
                    created_at = @CreatedAt,
                    last_used_at = @LastUsedAt
                WHERE id = @Id;";

            checkoutInfo.UpdatedAt = DateTime.UtcNow;

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@Id", checkoutInfo.Id);
            AddUserCheckoutInfoParameters(command, checkoutInfo);

            int rowEffected = await command.ExecuteNonQueryAsync();
            return new InfrastructureResponse<bool>()
            {
                IsSuccess = rowEffected > 0,
                Message = "The process completed successfully",
                Value = rowEffected > 0
            };
        }

        public async Task<InfrastructureResponse<bool>> DeleteAsync(int id)
        {
            const string query = @"DELETE FROM user_checkout_infos WHERE id = @Id";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);

            int rowEffected = await command.ExecuteNonQueryAsync();

            return new InfrastructureResponse<bool>()
            {
                IsSuccess = true,
                Message = "The process completed successfully",
                Value = rowEffected > 0
            };
        }

        public async Task<InfrastructureResponse<bool>> UpdateLastUsedAsync(int id)
        {
            const string query = @"UPDATE user_checkout_infos
                                   SET last_used_at = @LastUsedAt WHERE id = @Id;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@LastUsedAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("@Id", id);

            int rowEffected = await command.ExecuteNonQueryAsync();

            return new InfrastructureResponse<bool>()
            {
                IsSuccess = true,
                Message = "The process completed successfully",
                Value = rowEffected > 0
            };
        }

        private async Task<NpgsqlConnection> OpenConnectionAsync()
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        private UserCheckoutInfo MapUserCheckoutInfo(NpgsqlDataReader reader)
        {
            string? NullableString(string col)
            {
                int ord = reader.GetOrdinal(col);
                return reader.IsDBNull(ord) ? null : reader.GetString(ord);
            }

            return new UserCheckoutInfo
            {
                Id = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("id"))),
                UserId = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("user_id"))),
                SessionId = NullableString("session_id"),
                FirstName = NullableString("firstname") ?? string.Empty,
                LastName = NullableString("lastname") ?? string.Empty,
                Email = NullableString("email") ?? string.Empty,
                Phone = NullableString("phone"),
                Address = NullableString("address") ?? string.Empty,
                Address2 = NullableString("address2"),
                City = NullableString("city") ?? string.Empty,
                State = NullableString("state") ?? string.Empty,
                ZipCode = NullableString("zipcode") ?? string.Empty,
                Country = NullableString("country") ?? string.Empty,
                PayerEmail = NullableString("payer_email"),
                PayerName = NullableString("payer_name"),
                BillingAddress1 = NullableString("billing_address1"),
                BillingAddress2 = NullableString("billing_address2"),
                BillingCity = NullableString("billing_city"),
                BillingState = NullableString("billing_state"),
                BillingPostalCode = NullableString("billing_postal_code"),
                BillingCountry = NullableString("billing_country"),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at")),
                LastUsedAt = reader.IsDBNull(reader.GetOrdinal("last_used_at")) ? null : reader.GetDateTime(reader.GetOrdinal("last_used_at"))
            };
        }

        private void AddUserCheckoutInfoParameters(NpgsqlCommand command, UserCheckoutInfo checkoutInfo)
        {
            command.Parameters.AddWithValue("@UserId", checkoutInfo.UserId);
            command.Parameters.AddWithValue("@SessionId", (object?)checkoutInfo.SessionId ?? DBNull.Value);
            command.Parameters.AddWithValue("@FirstName", checkoutInfo.FirstName ?? string.Empty);
            command.Parameters.AddWithValue("@LastName", checkoutInfo.LastName ?? string.Empty);
            command.Parameters.AddWithValue("@Email", checkoutInfo.Email ?? string.Empty);
            command.Parameters.AddWithValue("@Phone", (object?)checkoutInfo.Phone ?? DBNull.Value);
            command.Parameters.AddWithValue("@PayerEmail", (object?)checkoutInfo.PayerEmail ?? DBNull.Value);
            command.Parameters.AddWithValue("@BillingCity", (object?)checkoutInfo.BillingCity ?? DBNull.Value);
            command.Parameters.AddWithValue("@BillingState", (object?)checkoutInfo.BillingState ?? DBNull.Value);
            command.Parameters.AddWithValue("@BillingCountry", (object?)checkoutInfo.BillingCountry ?? DBNull.Value);
            command.Parameters.AddWithValue("@UpdatedAt", checkoutInfo.UpdatedAt == default ? DateTime.UtcNow : checkoutInfo.UpdatedAt);
            command.Parameters.AddWithValue("@Address", checkoutInfo.Address ?? string.Empty);
            command.Parameters.AddWithValue("@Address2", (object?)checkoutInfo.Address2 ?? DBNull.Value);
            command.Parameters.AddWithValue("@City", checkoutInfo.City ?? string.Empty);
            command.Parameters.AddWithValue("@State", checkoutInfo.State ?? string.Empty);
            command.Parameters.AddWithValue("@ZipCode", checkoutInfo.ZipCode ?? string.Empty);
            command.Parameters.AddWithValue("@Country", checkoutInfo.Country ?? string.Empty);
            command.Parameters.AddWithValue("@PayerName", (object?)checkoutInfo.PayerName ?? DBNull.Value);
            command.Parameters.AddWithValue("@BillingAddress1", (object?)checkoutInfo.BillingAddress1 ?? DBNull.Value);
            command.Parameters.AddWithValue("@BillingAddress2", (object?)checkoutInfo.BillingAddress2 ?? DBNull.Value);
            command.Parameters.AddWithValue("@BillingPostalCode", (object?)checkoutInfo.BillingPostalCode ?? DBNull.Value);
            command.Parameters.AddWithValue("@CreatedAt", checkoutInfo.CreatedAt == default ? DateTime.UtcNow : checkoutInfo.CreatedAt);
            command.Parameters.AddWithValue("@LastUsedAt", (object?)checkoutInfo.LastUsedAt ?? DBNull.Value);
        }
    }
}