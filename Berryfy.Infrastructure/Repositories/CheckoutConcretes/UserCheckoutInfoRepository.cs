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
            string query = @"SELECT * from UserCheckoutInfos where user_id = @UserId";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@UserId", userId);

            UserCheckoutInfo checkoutInfo = new UserCheckoutInfo();

            var reader = await command.ExecuteReaderAsync();

            if(await reader.ReadAsync())
            {
                checkoutInfo = MapUserCheckoutInfo(reader);
            }

            return new InfrastructureResponse<UserCheckoutInfo?>()
            {
                IsSuccess = true,
                Message = "The process completed successfully",
                Value = checkoutInfo
            };
        }

        public async Task<InfrastructureResponse<UserCheckoutInfo?>> GetBySessionIdAsync(string sessionId)
        {
            string query = @"SELECT * from UserCheckoutInfos where session_id = @sessionId";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@sessionId", sessionId);

            UserCheckoutInfo checkoutInfo = new UserCheckoutInfo();

            var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                checkoutInfo = MapUserCheckoutInfo(reader);     
            }

            return new InfrastructureResponse<UserCheckoutInfo?>()
            {
                IsSuccess = true,
                Message = "The process completed successfully",
                Value = checkoutInfo
            };
        }

        public async Task<InfrastructureResponse<UserCheckoutInfo>> CreateAsync(UserCheckoutInfo checkoutInfo)
        {
            string query = @"Insert into user_checkout_infos (user_id, session_id, firstname, lastname, email,
phone, payer_email, billing_city, billing_state, billing_country, updated_at, address, address2, city, state, zipcode,
country, payer_name, billing_address1, billing_address2, billing_postal_code, created_at, last_used_at) values (@UserId,
@SessionId, @FirstName, @LastName, @Email, @Phone, @PayerEmail, @BillingCity, @BillingState, @BillingCountry,
@UpdatedAt, @Address, @Address2, @City, @State, @ZipCode, @Country, @PayerName, @BillingAddress1, @BillingAddress2,
@BillingPostalCode, @CreatedAt, @LastUsedAt) Returning *;";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(query, connection);

            AddUserCheckoutInfoParameters(command, checkoutInfo);

            var reader = await command.ExecuteReaderAsync();
            UserCheckoutInfo checkoutInfoObj = new UserCheckoutInfo();

            if(await reader.ReadAsync())
            {
                checkoutInfoObj = MapUserCheckoutInfo(reader);
            }

            return new InfrastructureResponse<UserCheckoutInfo>()
            {
                IsSuccess = true,
                Message = "The process completed successfully",
                Value = checkoutInfoObj
            };
        }

        public async Task<InfrastructureResponse<bool>> UpdateAsync(UserCheckoutInfo checkoutInfo)
        {
            string query = @"Update UserCheckoutInfos 
Set user_id = @UserId,
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
address = @Address
address2 = @Address2,
city = @City,
state= @State,
zipcode = @ZipCode,
country = @Country,
payer_name = @PayerName,
billing_address1 = @BillingAddress1,
billing_address2 = @BillingAddress2,
billing_postal_code = @BillingPostalCode,
created_at = @CreatedAt,
lastused_at = @LastUsedAt
where id = @Id;";

            var connection =await OpenConnectionAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@Id", checkoutInfo.Id);
            AddUserCheckoutInfoParameters(command, checkoutInfo);

            int rowEffected = await command.ExecuteNonQueryAsync();
            return new InfrastructureResponse<bool>()
            {
                IsSuccess = true,
                Message = "The process completed successfully",
                Value = rowEffected > 0
            };
        }

        public async Task<InfrastructureResponse<bool>> DeleteAsync(int id)
        {
            string query = @"Delete from UserCheckoutInfos where id = @Id";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(query, connection);

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
            string query = @"Update UserCheckoutInfos 
Set lastused_at = @LastUsedAt where id = @Id;";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(query, connection);

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
            return new UserCheckoutInfo
            {
                Id = reader.GetInt16(reader.GetOrdinal("id")),
                UserId = reader.GetInt16(reader.GetOrdinal("user_id")),
                SessionId = reader.GetString(reader.GetOrdinal("session_id")),
                FirstName = reader.GetString(reader.GetOrdinal("firstname")),
                LastName = reader.GetString(reader.GetOrdinal("lastname")),
                Email = reader.GetString(reader.GetOrdinal("email")),
                Phone = reader.GetString(reader.GetOrdinal("phone")),
                Address = reader.GetString(reader.GetOrdinal("address")),
                Address2 = reader.GetString(reader.GetOrdinal("address2")),
                City = reader.GetString(reader.GetOrdinal("city")),
                State = reader.GetString(reader.GetOrdinal("state")),
                ZipCode = reader.GetString(reader.GetOrdinal("zipcode")),
                Country = reader.GetString(reader.GetOrdinal("country")),
                PayerEmail = reader.GetString(reader.GetOrdinal("payer_email")),
                PayerName = reader.GetString(reader.GetOrdinal("payer_name")),
                BillingAddress1 = reader.GetString(reader.GetOrdinal("billing_address1")),
                BillingAddress2 = reader.GetString(reader.GetOrdinal("billing_address2")),
                BillingCity = reader.GetString(reader.GetOrdinal("billing_city")),
                BillingState = reader.GetString(reader.GetOrdinal("billing_state")),
                BillingPostalCode = reader.GetString(reader.GetOrdinal("billing_postal_code")),
                BillingCountry = reader.GetString(reader.GetOrdinal("billing_country")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at")),
                LastUsedAt = reader.GetDateTime(reader.GetOrdinal("lastused_at"))
            };
        }

        private void AddUserCheckoutInfoParameters(NpgsqlCommand command, UserCheckoutInfo checkoutInfo)
        {
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
        }
    }
}