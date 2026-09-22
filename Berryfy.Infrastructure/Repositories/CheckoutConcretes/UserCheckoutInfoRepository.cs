using Berryfy.Domain.Entities.CheckoutEntities;
using Berryfy.Domain.Repositories.CheckoutInterfaces;
using Microsoft.EntityFrameworkCore;
using Berryfy.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Berryfy.Infrastructure.Repositories.CheckoutConcretes
{
    public class UserCheckoutInfoRepository : IUserCheckoutInfoRepository
    {
        private readonly string _connectionString;

        public UserCheckoutInfoRepository(IConfiguration config)
        {
            _connectionString = PostgresConnectionStrings.Resolve(config);
        }

        public async Task<UserCheckoutInfo?> GetByUserIdAsync(int userId)
        {
            string query = @"SELECT * from UserCheckoutInfos where userId = @userId";

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@userId", userId);

            UserCheckoutInfo checkoutInfo = null;

            var reader = await command.ExecuteReaderAsync();

            while(await reader.ReadAsync())
            {
                checkoutInfo = new UserCheckoutInfo
                {
                    Id = reader.GetInt16(reader.GetOrdinal("Id")),
                    UserId = reader.GetInt16(reader.GetOrdinal("UserId")),
                    SessionId = reader.GetString(reader.GetOrdinal("SessionId")),
                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                    LastName = reader.GetString(reader.GetOrdinal("LastName")),
                    Email = reader.GetString(reader.GetOrdinal("LastName")),
                    Phone = reader.GetString(reader.GetOrdinal("Phone")),
                    Address = reader.GetString(reader.GetOrdinal("Address")),
                    Address2 = reader.GetString(reader.GetOrdinal("Address2")),
                    City = reader.GetString(reader.GetOrdinal("City")),
                    State = reader.GetString(reader.GetOrdinal("State")),
                    ZipCode = reader.GetString(reader.GetOrdinal("ZipCode")),
                    Country = reader.GetString(reader.GetOrdinal("Country")),
                    PayerEmail = reader.GetString(reader.GetOrdinal("PayerEmail")),
                    PayerName = reader.GetString(reader.GetOrdinal("PayerName")),
                    BillingAddress1 = reader.GetString(reader.GetOrdinal("BillingAddress1")),
                    BillingAddress2 = reader.GetString(reader.GetOrdinal("BillingAddress2")),
                    BillingCity = reader.GetString(reader.GetOrdinal("BillingCity")),
                    BillingState = reader.GetString(reader.GetOrdinal("BillingState")),
                    BillingPostalCode = reader.GetString(reader.GetOrdinal("BillingPostalCode")),
                    BillingCountry = reader.GetString(reader.GetOrdinal("BillingCountry")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                    LastUsedAt = reader.GetDateTime(reader.GetOrdinal("LastUsedAt"))
                };
            }

            return checkoutInfo;
        }

        public async Task<UserCheckoutInfo?> GetBySessionIdAsync(string sessionId)
        {
            string query = @"SELECT * from UserCheckoutInfos where SessionId = @sessionId";

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@sessionId", sessionId);

            UserCheckoutInfo checkoutInfo = null;

            var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                checkoutInfo = new UserCheckoutInfo
                {
                    Id = reader.GetInt16(reader.GetOrdinal("Id")),
                    UserId = reader.GetInt16(reader.GetOrdinal("UserId")),
                    SessionId = reader.GetString(reader.GetOrdinal("SessionId")),
                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                    LastName = reader.GetString(reader.GetOrdinal("LastName")),
                    Email = reader.GetString(reader.GetOrdinal("LastName")),
                    Phone = reader.GetString(reader.GetOrdinal("Phone")),
                    Address = reader.GetString(reader.GetOrdinal("Address")),
                    Address2 = reader.GetString(reader.GetOrdinal("Address2")),
                    City = reader.GetString(reader.GetOrdinal("City")),
                    State = reader.GetString(reader.GetOrdinal("State")),
                    ZipCode = reader.GetString(reader.GetOrdinal("ZipCode")),
                    Country = reader.GetString(reader.GetOrdinal("Country")),
                    PayerEmail = reader.GetString(reader.GetOrdinal("PayerEmail")),
                    PayerName = reader.GetString(reader.GetOrdinal("PayerName")),
                    BillingAddress1 = reader.GetString(reader.GetOrdinal("BillingAddress1")),
                    BillingAddress2 = reader.GetString(reader.GetOrdinal("BillingAddress2")),
                    BillingCity = reader.GetString(reader.GetOrdinal("BillingCity")),
                    BillingState = reader.GetString(reader.GetOrdinal("BillingState")),
                    BillingPostalCode = reader.GetString(reader.GetOrdinal("BillingPostalCode")),
                    BillingCountry = reader.GetString(reader.GetOrdinal("BillingCountry")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                    LastUsedAt = reader.GetDateTime(reader.GetOrdinal("LastUsedAt"))
                };
            }

            return checkoutInfo;
        }

        public async Task<UserCheckoutInfo> CreateAsync(UserCheckoutInfo checkoutInfo)
        {
            string query = @"Insert into UserCheckoutInfos (UserId, SessionId, FirstName, LastName, Email,
Phone, PayerEmail, BillingCity, BillingState, BillingCountry, UpdatedAt, Address, Address2, City, State, ZipCode,
Country, PayerName, BillingAddress1, BillingAddress2, BillingPostalCode, CreatedAt, LastUsedAt values (@UserId,
@SessionId, @FirstName, @LastName, @Email, @Phone, @PayerEmail, @BillingCity, @BillingState, @BillingCountry,
@UpdatedAt, @Address, @Address2, @City, @State, @ZipCode, @Country, @PayerName, @BillingAddress1, @BillingAddress2,
@BillingPostalCode, @CreatedAt, @LastUsedAt) Returning *;";

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@UserId", checkoutInfo.UserId);
            command.Parameters.AddWithValue("@SessionId", checkoutInfo.SessionId);
            command.Parameters.AddWithValue("@FirstName", checkoutInfo.FirstName);
            command.Parameters.AddWithValue("@LastName", checkoutInfo.LastName);
            command.Parameters.AddWithValue("@Email", checkoutInfo.Email);
            command.Parameters.AddWithValue("@Phone", checkoutInfo.Phone);
            command.Parameters.AddWithValue("@PayerEmail", checkoutInfo.PayerEmail);
            command.Parameters.AddWithValue("@BillingCity", checkoutInfo.BillingCity);
            command.Parameters.AddWithValue("@BillingState", checkoutInfo.BillingState);
            command.Parameters.AddWithValue("@BillingCountry", checkoutInfo.BillingCountry);
            command.Parameters.AddWithValue("@UpdatedAt", checkoutInfo.UpdatedAt);
            command.Parameters.AddWithValue("@Address", checkoutInfo.Address);
            command.Parameters.AddWithValue("@Address2", checkoutInfo.Address2);
            command.Parameters.AddWithValue("@City", checkoutInfo.City);
            command.Parameters.AddWithValue("@State", checkoutInfo.State);
            command.Parameters.AddWithValue("@ZipCode", checkoutInfo.ZipCode);
            command.Parameters.AddWithValue("@Country", checkoutInfo.Country);
            command.Parameters.AddWithValue("@PayerName", checkoutInfo.PayerName);
            command.Parameters.AddWithValue("@BillingAddress1", checkoutInfo.BillingAddress1);
            command.Parameters.AddWithValue("@BillingAddress2", checkoutInfo.BillingAddress2);
            command.Parameters.AddWithValue("@BillingPostalCode", checkoutInfo.BillingPostalCode);
            command.Parameters.AddWithValue("@CreatedAt", checkoutInfo.CreatedAt);
            command.Parameters.AddWithValue("@LastUsedAt", checkoutInfo.LastUsedAt);

            var reader = await command.ExecuteReaderAsync();
            UserCheckoutInfo checkoutInfoObj = null;

            while (await reader.ReadAsync())
            {
                checkoutInfoObj = new UserCheckoutInfo
                {
                    Id = reader.GetInt16(reader.GetOrdinal("Id")),
                    UserId = reader.GetInt16(reader.GetOrdinal("UserId")),
                    SessionId = reader.GetString(reader.GetOrdinal("SessionId")),
                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                    LastName = reader.GetString(reader.GetOrdinal("LastName")),
                    Email = reader.GetString(reader.GetOrdinal("LastName")),
                    Phone = reader.GetString(reader.GetOrdinal("Phone")),
                    Address = reader.GetString(reader.GetOrdinal("Address")),
                    Address2 = reader.GetString(reader.GetOrdinal("Address2")),
                    City = reader.GetString(reader.GetOrdinal("City")),
                    State = reader.GetString(reader.GetOrdinal("State")),
                    ZipCode = reader.GetString(reader.GetOrdinal("ZipCode")),
                    Country = reader.GetString(reader.GetOrdinal("Country")),
                    PayerEmail = reader.GetString(reader.GetOrdinal("PayerEmail")),
                    PayerName = reader.GetString(reader.GetOrdinal("PayerName")),
                    BillingAddress1 = reader.GetString(reader.GetOrdinal("BillingAddress1")),
                    BillingAddress2 = reader.GetString(reader.GetOrdinal("BillingAddress2")),
                    BillingCity = reader.GetString(reader.GetOrdinal("BillingCity")),
                    BillingState = reader.GetString(reader.GetOrdinal("BillingState")),
                    BillingPostalCode = reader.GetString(reader.GetOrdinal("BillingPostalCode")),
                    BillingCountry = reader.GetString(reader.GetOrdinal("BillingCountry")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                    LastUsedAt = reader.GetDateTime(reader.GetOrdinal("LastUsedAt"))
                };
            }

            return checkoutInfoObj;
        }

        public async Task<bool> UpdateAsync(UserCheckoutInfo checkoutInfo)
        {
            string query = @"Update UserCheckoutInfos 
Set UserId = @UserId,
SessionId = @SessionId,
FirstName = @FirstName,
LastName = @LastName,
Email = @Email,
Phone = @Phone,
PayerEmail = @PayerEmail,
BillingCity = @BillingCity,
BillingState = @BillingState,
BillingCountry = @BillingCountry,
UpdatedAt = @UpdatedAt,
Address = @Address
Address2 = @Address2,
City = @City,
State= @State,
ZipCode = @ZipCode,
Country = @Country,
PayerName = @PayerName,
BillingAddress1 = @BillingAddress1,
BillingAddress2 = @BillingAddress2,
BillingPostalCode = @BillingPostalCode,
CreatedAt = @CreatedAt,
LastUsedAt = @LastUsedAt
where Id = @Id;";

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@Id", checkoutInfo.Id);
            command.Parameters.AddWithValue("@UserId", checkoutInfo.UserId);
            command.Parameters.AddWithValue("@SessionId", checkoutInfo.SessionId);
            command.Parameters.AddWithValue("@FirstName", checkoutInfo.FirstName);
            command.Parameters.AddWithValue("@LastName", checkoutInfo.LastName);
            command.Parameters.AddWithValue("@Email", checkoutInfo.Email);
            command.Parameters.AddWithValue("@Phone", checkoutInfo.Phone);
            command.Parameters.AddWithValue("@PayerEmail", checkoutInfo.PayerEmail);
            command.Parameters.AddWithValue("@BillingCity", checkoutInfo.BillingCity);
            command.Parameters.AddWithValue("@BillingState", checkoutInfo.BillingState);
            command.Parameters.AddWithValue("@BillingCountry", checkoutInfo.BillingCountry);
            command.Parameters.AddWithValue("@UpdatedAt", checkoutInfo.UpdatedAt);
            command.Parameters.AddWithValue("@Address", checkoutInfo.Address);
            command.Parameters.AddWithValue("@Address2", checkoutInfo.Address2);
            command.Parameters.AddWithValue("@City", checkoutInfo.City);
            command.Parameters.AddWithValue("@State", checkoutInfo.State);
            command.Parameters.AddWithValue("@ZipCode", checkoutInfo.ZipCode);
            command.Parameters.AddWithValue("@Country", checkoutInfo.Country);
            command.Parameters.AddWithValue("@PayerName", checkoutInfo.PayerName);
            command.Parameters.AddWithValue("@BillingAddress1", checkoutInfo.BillingAddress1);
            command.Parameters.AddWithValue("@BillingAddress2", checkoutInfo.BillingAddress2);
            command.Parameters.AddWithValue("@BillingPostalCode", checkoutInfo.BillingPostalCode);
            command.Parameters.AddWithValue("@CreatedAt", checkoutInfo.CreatedAt);
            command.Parameters.AddWithValue("@LastUsedAt", checkoutInfo.LastUsedAt);


            int rowEffected = await command.ExecuteNonQueryAsync();

            return rowEffected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            string query = @"Delete from UserCheckoutInfos where Id = @Id";

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@Id", id);

            int rowEffected = await command.ExecuteNonQueryAsync();

            return rowEffected > 0;
        }

        public async Task<bool> UpdateLastUsedAsync(int id)
        {
            string query = @"Update UserCheckoutInfos 
Set LastUsedAt = @LastUsedAt where Id = @Id;";

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@LastUsedAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("@Id", id);

            int rowEffected = await command.ExecuteNonQueryAsync();

            return rowEffected > 0;
        }
    }
}
