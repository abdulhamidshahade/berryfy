using Berryfy.Domain.Entities.AuthEntities;
using Berryfy.Domain.Repositories.AuthInterfaces;
using Berryfy.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Berryfy.Infrastructure.Repositories.AuthConcretes
{
    public class UserRepository : IUserRepository
    {
        private readonly string _connectionString;

        public UserRepository(IConfiguration config)
        {
            _connectionString = PostgresConnectionStrings.Resolve(config);
        }

        public async Task<User> CreateAsync(User user)
        {
            const string sql = @"
                INSERT INTO users(
                    user_name, normalized_user_name, email, normalized_email, email_confirmed, password_hash,
                    security_stamp, concurrency_stamp, phone_number, phone_number_confirmed, twofactor_enabled,
                    lockout_end, lockout_enabled, access_failed_count, firstname, lastname, email_confirmation_code,
                    email_confirmation_code_expiry, password_reset_code, password_reset_code_expiry, refresh_token, refresh_token_expiry)
                VALUES(@UserName, @NormalizedUserName, @Email, @NormalizedEmail, @EmailConfirmed, @PasswordHash,
                    @SecurityStamp, @ConcurrencyStamp, @PhoneNumber, @PhoneNumberConfirmed, @TwoFactorEnabled,
                    @LockoutEnd, @LockoutEnabled, @AccessFailedCount, @FirstName, @LastName, @EmailConfirmationCode,
                    @EmailConfirmationCodeExpiry, @PasswordResetCode, @PasswordResetCodeExpiry, @RefreshToken, @RefreshTokenExpiry)
                RETURNING id;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            AddUserParameters(command, user);
            user.Id = Convert.ToInt32(await command.ExecuteScalarAsync());
            return user;
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            const string sql = "SELECT * FROM users WHERE id = @Id LIMIT 1";
            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("Id", id);
            await using var reader = await command.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapUser(reader) : null;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            const string sql = "SELECT * FROM users WHERE email = @Email LIMIT 1";
            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("Email", email);
            await using var reader = await command.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapUser(reader) : null;
        }

        public async Task<User?> GetByNormalizedEmailAsync(string normalizedEmail)
        {
            const string sql = "SELECT * FROM users WHERE normalized_email = @NormalizedEmail LIMIT 1";
            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("NormalizedEmail", normalizedEmail);
            await using var reader = await command.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapUser(reader) : null;
        }

        public async Task<User?> GetByRefreshTokenAsync(string refreshTokenHash)
        {
            const string sql = "SELECT * FROM users WHERE refresh_token = @RefreshToken LIMIT 1";
            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("RefreshToken", refreshTokenHash);
            await using var reader = await command.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapUser(reader) : null;
        }

        public async Task<List<User>> GetAllAsync()
        {
            const string sql = "SELECT * FROM users ORDER BY id";
            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();
            var users = new List<User>();
            while (await reader.ReadAsync())
            {
                users.Add(MapUser(reader));
            }

            return users;
        }

        public async Task<bool> UpdateAsync(User user)
        {
            const string sql = @"
                UPDATE users SET
                    user_name = @UserName,
                    normalized_user_name = @NormalizedUserName,
                    email = @Email,
                    normalized_email = @NormalizedEmail,
                    email_confirmed = @EmailConfirmed,
                    password_hash = @PasswordHash,
                    security_stamp = @SecurityStamp,
                    concurrency_stamp = @ConcurrencyStamp,
                    phone_number = @PhoneNumber,
                    phone_number_confirmed = @PhoneNumberConfirmed,
                    twofactor_enabled = @TwoFactorEnabled,
                    lockout_end = @LockoutEnd,
                    lockout_enabled = @LockoutEnabled,
                    access_failed_count = @AccessFailedCount,
                    firstname = @FirstName,
                    lastname = @LastName,
                    email_confirmation_code = @EmailConfirmationCode,
                    email_confirmation_code_expiry = @EmailConfirmationCodeExpiry,
                    password_reset_code = @PasswordResetCode,
                    password_reset_code_expiry = @PasswordResetCodeExpiry,
                    refresh_token = @RefreshToken,
                    refresh_token_expiry = @RefreshTokenExpiry
                WHERE id = @Id";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            AddUserParameters(command, user);
            command.Parameters.AddWithValue("Id", user.Id);
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "DELETE FROM users WHERE id = @Id";
            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("Id", id);
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> ExistsByIdAsync(int id)
        {
            const string sql = "SELECT COUNT(1) FROM users WHERE id = @Id";
            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("Id", id);
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            const string sql = "SELECT COUNT(1) FROM users WHERE normalized_email = @NormalizedEmail";
            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("NormalizedEmail", email);
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        public async Task<bool> IsUsernameTakenAsync(string userName)
        {
            const string sql = "SELECT COUNT(1) FROM users WHERE normalized_user_name = @NormalizedUserName";
            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("NormalizedUserName", Normalize(userName));
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        public async Task<bool> SetLockoutAsync(int userId, DateTime? lockoutEnd)
        {
            const string sql = "UPDATE users SET lockout_end = @LockoutEnd WHERE id = @Id";
            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("LockoutEnd", (object?)lockoutEnd ?? DBNull.Value);
            command.Parameters.AddWithValue("Id", userId);
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> ResetAccessFailedCountAsync(int userId)
        {
            const string sql = "UPDATE users SET access_failed_count = 0 WHERE id = @Id";
            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("Id", userId);
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> IncrementAccessFailedCountAsync(int userId)
        {
            const string sql = "UPDATE users SET access_failed_count = access_failed_count + 1 WHERE id = @Id";
            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("Id", userId);
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> UpdatePasswordHashAsync(int userId, string passwordHash)
        {
            const string sql = "UPDATE users SET password_hash = @PasswordHash WHERE id = @Id";
            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("PasswordHash", passwordHash);
            command.Parameters.AddWithValue("Id", userId);
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> ConfirmEmailAsync(int userId)
        {
            const string sql = "UPDATE users SET email_confirmed = true WHERE id = @Id";
            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("Id", userId);
            return await command.ExecuteNonQueryAsync() > 0;
        }

        private async Task<NpgsqlConnection> OpenConnectionAsync()
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        private static void AddUserParameters(NpgsqlCommand command, User user)
        {
            command.Parameters.AddWithValue("@UserName", user.UserName ?? string.Empty);
            command.Parameters.AddWithValue("@NormalizedUserName", user.NormalizedUserName ?? string.Empty);
            command.Parameters.AddWithValue("@Email", user.Email ?? string.Empty);
            command.Parameters.AddWithValue("@NormalizedEmail", user.NormalizedEmail ?? string.Empty);
            command.Parameters.AddWithValue("@EmailConfirmed", user.EmailConfirmed);
            command.Parameters.AddWithValue("@PasswordHash", (object?)user.PasswordHash ?? DBNull.Value);
            command.Parameters.AddWithValue("@SecurityStamp", user.SecurityStamp ?? string.Empty);
            command.Parameters.AddWithValue("@ConcurrencyStamp", user.ConcurrencyStamp ?? string.Empty);
            command.Parameters.AddWithValue("@PhoneNumber", (object?)user.PhoneNumber ?? DBNull.Value);
            command.Parameters.AddWithValue("@PhoneNumberConfirmed", user.PhoneNumberConfirmed);
            command.Parameters.AddWithValue("@TwoFactorEnabled", user.TwoFactorEnabled);
            command.Parameters.AddWithValue("@LockoutEnd", (object?)user.LockoutEnd ?? DBNull.Value);
            command.Parameters.AddWithValue("@LockoutEnabled", user.LockoutEnabled);
            command.Parameters.AddWithValue("@AccessFailedCount", user.AccessFailedCount);
            command.Parameters.AddWithValue("@FirstName", user.FirstName ?? string.Empty);
            command.Parameters.AddWithValue("@LastName", user.LastName ?? string.Empty);
            command.Parameters.AddWithValue("@EmailConfirmationCode", (object?)user.EmailConfirmationCode ?? DBNull.Value);
            command.Parameters.AddWithValue("@EmailConfirmationCodeExpiry", (object?)user.EmailConfirmationCodeExpiry ?? DBNull.Value);
            command.Parameters.AddWithValue("@PasswordResetCode", (object?)user.PasswordResetCode ?? DBNull.Value);
            command.Parameters.AddWithValue("@PasswordResetCodeExpiry", (object?)user.PasswordResetCodeExpiry ?? DBNull.Value);
            command.Parameters.AddWithValue("@RefreshToken", (object?)user.RefreshToken ?? DBNull.Value);
            command.Parameters.AddWithValue("@RefreshTokenExpiry", (object?)user.RefreshTokenExpiry ?? DBNull.Value);
        }

        private static User MapUser(NpgsqlDataReader reader)
        {
            return new User
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                FirstName = GetString(reader, "firstname"),
                LastName = GetString(reader, "lastname"),
                UserName = GetString(reader, "user_name"),
                NormalizedUserName = GetString(reader, "normalized_user_name"),
                Email = GetString(reader, "email"),
                NormalizedEmail = GetString(reader, "normalized_email"),
                EmailConfirmed = GetBoolean(reader, "email_confirmed"),
                PasswordHash = GetString(reader, "password_hash"),
                SecurityStamp = GetString(reader, "security_stamp"),
                ConcurrencyStamp = GetString(reader, "concurrency_stamp"),
                PhoneNumber = GetNullableString(reader, "phone_number"),
                PhoneNumberConfirmed = GetBoolean(reader, "phone_number_confirmed"),
                TwoFactorEnabled = GetBoolean(reader, "twofactor_enabled"),
                LockoutEnd = GetNullableDateTime(reader, "lockout_end"),
                LockoutEnabled = GetBoolean(reader, "lockout_enabled"),
                AccessFailedCount = reader.IsDBNull(reader.GetOrdinal("access_failed_count"))
                    ? 0
                    : reader.GetInt32(reader.GetOrdinal("access_failed_count")),
                EmailConfirmationCode = GetNullableString(reader, "email_confirmation_code"),
                EmailConfirmationCodeExpiry = GetNullableDateTime(reader, "email_confirmation_code_expiry"),
                PasswordResetCode = GetNullableString(reader, "password_reset_code"),
                PasswordResetCodeExpiry = GetNullableDateTime(reader, "password_reset_code_expiry"),
                RefreshToken = GetNullableString(reader, "refresh_token"),
                RefreshTokenExpiry = GetNullableDateTime(reader, "refresh_token_expiry")
            };
        }

        private static string Normalize(string value)
        {
            return value.Trim().ToUpperInvariant();
        }

        private static string GetString(NpgsqlDataReader reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
        }

        private static string? GetNullableString(NpgsqlDataReader reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }

        private static bool GetBoolean(NpgsqlDataReader reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return !reader.IsDBNull(ordinal) && reader.GetBoolean(ordinal);
        }

        private static DateTime? GetNullableDateTime(NpgsqlDataReader reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
        }
    }
}
