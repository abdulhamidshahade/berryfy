using Berryfy.Domain.Entities;
using Berryfy.Domain.Entities.CouponEntities;
using Berryfy.Domain.Repositories.CouponInterfaces;
using Berryfy.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Linq.Expressions;

namespace Berryfy.Infrastructure.Repositories.CouponConcretes
{
    public class CouponRepository : ICouponRepository
    {
        private readonly string _connectionString;

        public CouponRepository()
        {
            _connectionString = string.Empty;
        }

        public CouponRepository(IConfiguration config)
        {
            _connectionString = PostgresConnectionStrings.Resolve(config);
        }

        public async Task<InfrastructureResponse<IEnumerable<Coupon>>> GetAllAsync()
        {
            const string sql = "SELECT * FROM coupons ORDER BY id";
            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            var coupons = new List<Coupon>();
            while (await reader.ReadAsync())
            {
                coupons.Add(MapCoupon(reader));
            }

            return new InfrastructureResponse<IEnumerable<Coupon>>()
            {
                IsSuccess = true,
                Message = "The process completed successfully",
                Value = coupons
            };
        }

        public async Task<InfrastructureResponse<Coupon?>> GetByCodeAsync(string code)
        {
            const string sql = "SELECT * FROM coupons WHERE code = @Code LIMIT 1";
            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Code", code);
            await using var reader = await command.ExecuteReaderAsync();
            var coupon = await reader.ReadAsync() ? MapCoupon(reader) : null;
            return new InfrastructureResponse<Coupon?>()
            {
                IsSuccess = true,
                Message = "The process completed successfully",
                Value = coupon
            };
        }

        public async Task<InfrastructureResponse<Coupon?>> GetByIdAsync(int id)
        {
            const string sql = "SELECT * FROM coupons WHERE id = @Id LIMIT 1";
            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);
            await using var reader = await command.ExecuteReaderAsync();
            var coupon = await reader.ReadAsync() ? MapCoupon(reader) : null;
            return new InfrastructureResponse<Coupon?>()
            {
                IsSuccess = true,
                Message = "The process completed successfully",
                Value = coupon
            };
        }

        public async Task<InfrastructureResponse<Coupon>> CreateAsync(Coupon coupon)
        {
            const string sql = @"
                INSERT INTO coupons (
                    code, description, discount_amount, minimum_order_amount, is_active,
                    created_at, updated_at, type, value, is_for_new_users_only)
                VALUES (
                    @Code, @Description, @DiscountAmount, @MinimumOrderAmount, @IsActive,
                    @CreatedAt, @UpdatedAt, @Type, @Value, @IsForNewUsersOnly)
                RETURNING id;";

            var now = DateTime.UtcNow;
            coupon.CreatedAt = now;
            coupon.UpdatedAt = now;

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            AddCouponParameters(command, coupon);
            coupon.Id = Convert.ToInt32(await command.ExecuteScalarAsync());
            return new InfrastructureResponse<Coupon>()
            {
                IsSuccess = true,
                Message = "The process completed successfully",
                Value = coupon
            };
        }

        public async Task<InfrastructureResponse<Coupon?>> UpdateAsync(int id, Coupon coupon)
        {
            const string sql = @"
                UPDATE coupons SET
                    code = @Code,
                    description = @Description,
                    discount_amount = @DiscountAmount,
                    minimum_order_amount = @MinimumOrderAmount,
                    is_active = @IsActive,
                    updated_at = @UpdatedAt,
                    type = @Type,
                    value = @Value,
                    is_for_new_users_only = @IsForNewUsersOnly
                WHERE id = @Id";

            coupon.UpdatedAt = DateTime.UtcNow;

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            AddCouponParameters(command, coupon);
            command.Parameters.AddWithValue("@Id", id);

            if (await command.ExecuteNonQueryAsync() == 0)
            {
                return new InfrastructureResponse<Coupon?>()
                {
                    IsSuccess = false,
                    Message = "The process failed",
                    Value = null
                };
            }

            return new InfrastructureResponse<Coupon?>()
            {
                IsSuccess = true,
                Message = "The process completed successfully",
                Value = GetByIdAsync(id).GetAwaiter().GetResult().Value
            };
        }

        public async Task<InfrastructureResponse<bool>> DeleteAsync(Coupon coupon)
        {
            const string sql = "DELETE FROM coupons WHERE id = @Id";
            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", coupon.Id);
            return new InfrastructureResponse<bool>()
            {
                IsSuccess = true,
                Message = "The process completed successfully",
                Value = await command.ExecuteNonQueryAsync() > 0
            };
        }

        public async Task<InfrastructureResponse<bool>> ExistsAsync(Expression<Func<Coupon, bool>> expression)
        {
            var coupons = GetAllAsync().GetAwaiter().GetResult().Value;
            return new InfrastructureResponse<bool>()
            {
                IsSuccess = true,
                Message = "The process completed successfully",
                Value = coupons.AsQueryable().Any(expression)
            };
        }

        private async Task<NpgsqlConnection> OpenConnectionAsync()
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException("CouponRepository is not configured with a PostgreSQL connection string.");
            }
            
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        private static void AddCouponParameters(NpgsqlCommand command, Coupon coupon)
        {
            command.Parameters.AddWithValue("@Code", coupon.Code);
            command.Parameters.AddWithValue("@Description", coupon.Description ?? string.Empty);
            command.Parameters.AddWithValue("@DiscountAmount", coupon.DiscountAmount);
            command.Parameters.AddWithValue("@MinimumOrderAmount", coupon.MinimumOrderAmount);
            command.Parameters.AddWithValue("@IsActive", coupon.IsActive);
            command.Parameters.AddWithValue("@CreatedAt", coupon.CreatedAt);
            command.Parameters.AddWithValue("@UpdatedAt", coupon.UpdatedAt);
            command.Parameters.AddWithValue("@Type", (int)coupon.Type);
            command.Parameters.AddWithValue("@Value", coupon.Value);
            command.Parameters.AddWithValue("@IsForNewUsersOnly", coupon.IsForNewUsersOnly);
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
    }
}