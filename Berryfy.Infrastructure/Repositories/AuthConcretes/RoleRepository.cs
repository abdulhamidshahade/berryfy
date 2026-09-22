using Berryfy.Domain.Entities.AuthEntities;
using Berryfy.Domain.Repositories.AuthInterfaces;
using Berryfy.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Berryfy.Infrastructure.Repositories.AuthConcretes
{
    public class RoleRepository : IRoleRepository
    {
        private readonly string _connectionString;

        public RoleRepository(IConfiguration config)
        {
            _connectionString = PostgresConnectionStrings.Resolve(config);
        }

        public async Task<bool> RoleExistsAsync(string roleName)
        {
            const string sql = "SELECT COUNT(1) FROM roles WHERE normalized_name = @NormalizedName";
            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("NormalizedName", Normalize(roleName));
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        public async Task<Role?> GetByNameAsync(string roleName)
        {
            const string sql = "SELECT id, name, normalized_name, concurrency_stamp FROM roles WHERE normalized_name = @NormalizedName";
            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("NormalizedName", Normalize(roleName));
            await using var reader = await command.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapRole(reader) : null;
        }

        public async Task<Role> CreateAsync(Role role)
        {
            const string sql = @"
                INSERT INTO roles (name, normalized_name, concurrency_stamp)
                VALUES (@Name, @NormalizedName, @ConcurrencyStamp)
                ON CONFLICT (normalized_name) DO UPDATE SET name = EXCLUDED.name
                RETURNING id, name, normalized_name, concurrency_stamp;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("Name", role.Name);
            command.Parameters.AddWithValue("NormalizedName", string.IsNullOrWhiteSpace(role.NormalizedName) ? Normalize(role.Name) : role.NormalizedName);
            command.Parameters.AddWithValue("ConcurrencyStamp", string.IsNullOrWhiteSpace(role.ConcurrencyStamp) ? Guid.NewGuid().ToString() : role.ConcurrencyStamp);
            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapRole(reader);
            }

            return role;
        }

        public async Task<bool> DeleteAsync(string roleName)
        {
            const string sql = "DELETE FROM roles WHERE normalized_name = @NormalizedName";
            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("NormalizedName", Normalize(roleName));
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> UpdateAsync(string oldRoleName, string newRoleName)
        {
            const string sql = @"
                UPDATE roles
                SET name = @NewName,
                    normalized_name = @NewNormalizedName,
                    concurrency_stamp = @ConcurrencyStamp
                WHERE normalized_name = @OldNormalizedName;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("NewName", newRoleName);
            command.Parameters.AddWithValue("NewNormalizedName", Normalize(newRoleName));
            command.Parameters.AddWithValue("ConcurrencyStamp", Guid.NewGuid().ToString());
            command.Parameters.AddWithValue("OldNormalizedName", Normalize(oldRoleName));
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<List<Role>> GetAllAsync()
        {
            const string sql = "SELECT id, name, normalized_name, concurrency_stamp FROM roles ORDER BY name";
            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();
            var roles = new List<Role>();
            while (await reader.ReadAsync())
            {
                roles.Add(MapRole(reader));
            }

            return roles;
        }

        public async Task<bool> AssignRoleToUserAsync(int userId, string roleName)
        {
            const string sql = @"
                INSERT INTO user_roles (user_id, role_id)
                SELECT @UserId, r.id
                FROM roles r
                WHERE r.normalized_name = @NormalizedName
                ON CONFLICT (user_id, role_id) DO NOTHING;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("UserId", userId);
            command.Parameters.AddWithValue("NormalizedName", Normalize(roleName));
            return await command.ExecuteNonQueryAsync() >= 0;
        }

        public async Task<bool> RemoveRoleFromUserAsync(int userId, string roleName)
        {
            const string sql = @"
                DELETE FROM user_roles ur
                USING roles r
                WHERE ur.role_id = r.id
                  AND ur.user_id = @UserId
                  AND r.normalized_name = @NormalizedName;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("UserId", userId);
            command.Parameters.AddWithValue("NormalizedName", Normalize(roleName));
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> IsUserInRoleAsync(int userId, string roleName)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM user_roles ur
                JOIN roles r ON r.id = ur.role_id
                WHERE ur.user_id = @UserId AND r.normalized_name = @NormalizedName;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("UserId", userId);
            command.Parameters.AddWithValue("NormalizedName", Normalize(roleName));
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        public async Task<List<string>> GetUserRolesAsync(int userId)
        {
            const string sql = @"
                SELECT r.name
                FROM roles r
                JOIN user_roles ur ON ur.role_id = r.id
                WHERE ur.user_id = @UserId
                ORDER BY r.name;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("UserId", userId);
            await using var reader = await command.ExecuteReaderAsync();
            var roles = new List<string>();
            while (await reader.ReadAsync())
            {
                roles.Add(reader.GetString(0));
            }

            return roles;
        }

        public async Task<List<User>> GetUsersInRoleAsync(string roleName)
        {
            const string sql = @"
                SELECT u.id, u.user_name, u.normalized_user_name, u.email, u.normalized_email,
                       u.email_confirmed, u.firstname, u.lastname
                FROM users u
                JOIN user_roles ur ON ur.user_id = u.id
                JOIN roles r ON r.id = ur.role_id
                WHERE r.normalized_name = @NormalizedName
                ORDER BY u.id;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("NormalizedName", Normalize(roleName));
            await using var reader = await command.ExecuteReaderAsync();
            var users = new List<User>();
            while (await reader.ReadAsync())
            {
                users.Add(new User
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    UserName = GetString(reader, "user_name"),
                    NormalizedUserName = GetString(reader, "normalized_user_name"),
                    Email = GetString(reader, "email"),
                    NormalizedEmail = GetString(reader, "normalized_email"),
                    EmailConfirmed = GetBoolean(reader, "email_confirmed"),
                    FirstName = GetString(reader, "firstname"),
                    LastName = GetString(reader, "lastname")
                });
            }

            return users;
        }

        public async Task<List<(User User, List<string> Roles)>> GetAllUsersWithRolesAsync()
        {
            const string sql = @"
                SELECT u.id, u.user_name, u.normalized_user_name, u.email, u.normalized_email,
                       u.email_confirmed, u.firstname, u.lastname, u.lockout_end, u.access_failed_count,
                       r.name AS role_name
                FROM users u
                LEFT JOIN user_roles ur ON ur.user_id = u.id
                LEFT JOIN roles r ON r.id = ur.role_id
                ORDER BY u.id, r.name;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();
            var users = new Dictionary<int, (User User, List<string> Roles)>();

            while (await reader.ReadAsync())
            {
                var userId = reader.GetInt32(reader.GetOrdinal("id"));
                if (!users.TryGetValue(userId, out var entry))
                {
                    entry = (new User
                    {
                        Id = userId,
                        UserName = GetString(reader, "user_name"),
                        NormalizedUserName = GetString(reader, "normalized_user_name"),
                        Email = GetString(reader, "email"),
                        NormalizedEmail = GetString(reader, "normalized_email"),
                        EmailConfirmed = GetBoolean(reader, "email_confirmed"),
                        FirstName = GetString(reader, "firstname"),
                        LastName = GetString(reader, "lastname"),
                        LockoutEnd = GetNullableDateTime(reader, "lockout_end"),
                        AccessFailedCount = reader.GetInt32(reader.GetOrdinal("access_failed_count"))
                    }, new List<string>());
                    users[userId] = entry;
                }

                var roleOrdinal = reader.GetOrdinal("role_name");
                if (!reader.IsDBNull(roleOrdinal))
                {
                    entry.Roles.Add(reader.GetString(roleOrdinal));
                }
            }

            return users.Values.ToList();
        }

        public async Task<(int TotalRoles, int TotalUsers, int UsersWithRoles, int UsersWithoutRoles)> GetRoleStatsAsync()
        {
            const string sql = @"
                SELECT
                    (SELECT COUNT(1) FROM roles) AS total_roles,
                    (SELECT COUNT(1) FROM users) AS total_users,
                    (SELECT COUNT(DISTINCT user_id) FROM user_roles) AS users_with_roles;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return (0, 0, 0, 0);
            }

            var totalRoles = reader.GetInt32(reader.GetOrdinal("total_roles"));
            var totalUsers = reader.GetInt32(reader.GetOrdinal("total_users"));
            var usersWithRoles = reader.GetInt32(reader.GetOrdinal("users_with_roles"));
            return (totalRoles, totalUsers, usersWithRoles, totalUsers - usersWithRoles);
        }

        private async Task<NpgsqlConnection> OpenConnectionAsync()
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        private static Role MapRole(NpgsqlDataReader reader)
        {
            return new Role
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                Name = GetString(reader, "name"),
                NormalizedName = GetString(reader, "normalized_name"),
                ConcurrencyStamp = GetString(reader, "concurrency_stamp")
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
