using Berryfy.Domain.Entities.AuthEntities;
using Berryfy.Domain.Entities.CouponEntities;
using Berryfy.Domain.Repositories.CouponInterfaces;
using Berryfy.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Berryfy.Infrastructure.Repositories.CouponConcretes
{
    public class UserCouponRepository : IUserCouponRepository
    {
        private readonly string _connectionString;

        public UserCouponRepository(IConfiguration config)
        {
            _connectionString = PostgresConnectionStrings.Resolve(config);
        }

        public async Task<UserCoupon> AddCouponToUserAsync(int userId, int couponId)
        {
            const string findSql = @"
                SELECT id, user_id, coupon_id, is_used, used_at, order_id, created_at, updated_at
                FROM user_coupons
                WHERE user_id = @UserId AND coupon_id = @CouponId
                LIMIT 1";

            await using var connection = await OpenConnectionAsync();
            await using (var findCommand = new NpgsqlCommand(findSql, connection))
            {
                findCommand.Parameters.AddWithValue("UserId", userId);
                findCommand.Parameters.AddWithValue("CouponId", couponId);
                await using var reader = await findCommand.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapUserCoupon(reader);
                }
            }

            const string insertSql = @"
                INSERT INTO user_coupons (user_id, coupon_id, is_used, created_at, updated_at)
                VALUES (@UserId, @CouponId, false, @CreatedAt, @UpdatedAt)
                RETURNING id, user_id, coupon_id, is_used, used_at, order_id, created_at, updated_at";

            var now = DateTime.UtcNow;
            await using var insertCommand = new NpgsqlCommand(insertSql, connection);
            insertCommand.Parameters.AddWithValue("UserId", userId);
            insertCommand.Parameters.AddWithValue("CouponId", couponId);
            insertCommand.Parameters.AddWithValue("CreatedAt", now);
            insertCommand.Parameters.AddWithValue("UpdatedAt", now);

            await using var insertReader = await insertCommand.ExecuteReaderAsync();
            if (await insertReader.ReadAsync())
            {
                return MapUserCoupon(insertReader);
            }

            throw new InvalidOperationException("Failed to assign coupon to user.");
        }

        public Task<UserCoupon> AddUserToCouponAsync(int userId, int couponId)
        {
            return AddCouponToUserAsync(userId, couponId);
        }

        public async Task<bool> DisableCouponForUserAsync(int userId, int couponId)
        {
            const string sql = @"
                UPDATE user_coupons
                SET is_used = true, updated_at = @UpdatedAt
                WHERE user_id = @UserId AND coupon_id = @CouponId AND is_used = false";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("UserId", userId);
            command.Parameters.AddWithValue("CouponId", couponId);
            command.Parameters.AddWithValue("UpdatedAt", DateTime.UtcNow);
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<IReadOnlyList<Coupon>> GetCouponsByUserIdAsync(int userId)
        {
            const string sql = @"
                SELECT c.*
                FROM user_coupons uc
                INNER JOIN coupons c ON c.id = uc.coupon_id
                WHERE uc.user_id = @UserId
                ORDER BY c.id";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("UserId", userId);
            await using var reader = await command.ExecuteReaderAsync();

            var coupons = new List<Coupon>();
            while (await reader.ReadAsync())
            {
                coupons.Add(MapCoupon(reader));
            }

            return coupons;
        }

        public async Task<IReadOnlyList<User>> GetUsersByCouponIdAsync(int couponId)
        {
            const string sql = @"
                SELECT u.*
                FROM user_coupons uc
                INNER JOIN users u ON u.id = uc.user_id
                WHERE uc.coupon_id = @CouponId
                ORDER BY u.id";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("CouponId", couponId);
            await using var reader = await command.ExecuteReaderAsync();

            var users = new List<User>();
            while (await reader.ReadAsync())
            {
                users.Add(MapUser(reader));
            }

            return users;
        }

        public async Task<bool> IsCouponUsedByUserAsync(int userId, string couponCode)
        {
            const string sql = @"
                SELECT uc.is_used
                FROM user_coupons uc
                INNER JOIN coupons c ON c.id = uc.coupon_id
                WHERE uc.user_id = @UserId AND c.code = @CouponCode
                LIMIT 1";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("UserId", userId);
            command.Parameters.AddWithValue("CouponCode", couponCode);

            var result = await command.ExecuteScalarAsync();
            return result is bool isUsed && isUsed;
        }

        public async Task<bool> MarkCouponAsUsedAsync(int userId, int couponId, int orderId)
        {
            const string sql = @"
                UPDATE user_coupons
                SET is_used = true,
                    used_at = @UsedAt,
                    order_id = @OrderId,
                    updated_at = @UpdatedAt
                WHERE user_id = @UserId AND coupon_id = @CouponId
                  AND (is_used = false OR order_id = @OrderId)";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("UserId", userId);
            command.Parameters.AddWithValue("CouponId", couponId);
            command.Parameters.AddWithValue("OrderId", orderId);
            command.Parameters.AddWithValue("UsedAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("UpdatedAt", DateTime.UtcNow);
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> RevertCouponUsageAsync(int userId, int couponId, int orderId)
        {
            const string sql = @"
                UPDATE user_coupons
                SET is_used = false,
                    used_at = NULL,
                    order_id = NULL,
                    updated_at = @UpdatedAt
                WHERE user_id = @UserId AND coupon_id = @CouponId AND order_id = @OrderId";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("UserId", userId);
            command.Parameters.AddWithValue("CouponId", couponId);
            command.Parameters.AddWithValue("OrderId", orderId);
            command.Parameters.AddWithValue("UpdatedAt", DateTime.UtcNow);
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<List<int>> GetCouponIdsUsedInOrderAsync(int orderId)
        {
            const string sql = @"
                SELECT coupon_id
                FROM user_coupons
                WHERE order_id = @OrderId AND is_used = true";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("OrderId", orderId);
            await using var reader = await command.ExecuteReaderAsync();

            var couponIds = new List<int>();
            while (await reader.ReadAsync())
            {
                couponIds.Add(reader.GetInt32(0));
            }

            return couponIds;
        }

        private async Task<NpgsqlConnection> OpenConnectionAsync()
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        private static UserCoupon MapUserCoupon(NpgsqlDataReader reader)
        {
            return new UserCoupon
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                CouponId = reader.GetInt32(reader.GetOrdinal("coupon_id")),
                IsUsed = reader.GetBoolean(reader.GetOrdinal("is_used")),
                UsedAt = reader.IsDBNull(reader.GetOrdinal("used_at")) ? null : reader.GetDateTime(reader.GetOrdinal("used_at")),
                OrderId = reader.IsDBNull(reader.GetOrdinal("order_id")) ? null : reader.GetInt32(reader.GetOrdinal("order_id")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
            };
        }

        private static Coupon MapCoupon(NpgsqlDataReader reader)
        {
            return new Coupon
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                Code = reader.GetString(reader.GetOrdinal("code")),
                Description = reader.GetString(reader.GetOrdinal("description")),
                DiscountAmount = reader.GetDecimal(reader.GetOrdinal("discount_amount")),
                MinimumOrderAmount = reader.GetDecimal(reader.GetOrdinal("minimum_order_amount")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at")),
                Type = (Domain.Constants.CouponType)reader.GetInt32(reader.GetOrdinal("type")),
                Value = reader.GetDecimal(reader.GetOrdinal("value")),
                IsForNewUsersOnly = reader.GetBoolean(reader.GetOrdinal("is_for_new_users_only"))
            };
        }

        private static User MapUser(NpgsqlDataReader reader)
        {
            return new User
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                UserName = reader.IsDBNull(reader.GetOrdinal("user_name")) ? string.Empty : reader.GetString(reader.GetOrdinal("user_name")),
                Email = reader.IsDBNull(reader.GetOrdinal("email")) ? string.Empty : reader.GetString(reader.GetOrdinal("email")),
                FirstName = reader.IsDBNull(reader.GetOrdinal("firstname")) ? string.Empty : reader.GetString(reader.GetOrdinal("firstname")),
                LastName = reader.IsDBNull(reader.GetOrdinal("lastname")) ? string.Empty : reader.GetString(reader.GetOrdinal("lastname"))
            };
        }
    }
}
