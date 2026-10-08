using Berryfy.Domain.Constants;
using Berryfy.Domain.Entities.ShoppingCartEntities;
using Berryfy.Domain.Entities.ProductEntities;
using Berryfy.Domain.Entities.CouponEntities;
using Berryfy.Domain.Repositories.ShoppingCartInterfaces;
using Berryfy.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Berryfy.Domain.Entities;

namespace Berryfy.Infrastructure.Repositories.ShoppingCartConcretes
{
    public class CartRepository : ICartRepository
    {
        private readonly string _connectionString;

        public CartRepository(IConfiguration config)
        {
            _connectionString = PostgresConnectionStrings.Resolve(config);
        }

        public async Task<InfrastructureResponse<Cart>> CreateCartAsync(int? userId, CartStatus status)
        {
            const string sql = @"
                INSERT INTO shopping_carts (user_id, session_id, status, created_at, updated_at, version)
                VALUES (@UserId, @SessionId, @Status, @CreatedAt, @UpdatedAt, @Version)
                RETURNING id, user_id, session_id, status, created_at, updated_at, version";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
            command.Parameters.AddWithValue("@SessionId", (object?)null ?? DBNull.Value);
            command.Parameters.AddWithValue("@Status", status.ToString());
            command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("@Version", 1);

            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var cart = MapCart(reader);

                return new InfrastructureResponse<Cart>()
                {
                    IsSuccess = true,
                    Message = "Cart created successfully",
                    Value = cart
                };
            }

            return new InfrastructureResponse<Cart>()
            {
                IsSuccess = false,
                Message = "Failed to create cart",
                Value = null
            };
        }

        public async Task<InfrastructureResponse<Cart>> CreateCartAsync(string? sessionId, CartStatus status)
        {
            const string sql = @"
                INSERT INTO shopping_carts (user_id, session_id, status, created_at, updated_at, version)
                VALUES (@UserId, @SessionId, @Status, @CreatedAt, @UpdatedAt, @Version)
                RETURNING id, user_id, session_id, status, created_at, updated_at, version";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue(@"UserId", (object?)null ?? DBNull.Value);
            command.Parameters.AddWithValue("@SessionId", (object?)sessionId ?? DBNull.Value);
            command.Parameters.AddWithValue("@Status", status.ToString());
            command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("@Version", 1);

            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var cart = MapCart(reader);

                return new InfrastructureResponse<Cart>()
                {
                    IsSuccess = true,
                    Message = "Cart created successfully",
                    Value = cart
                };
            }

            return new InfrastructureResponse<Cart>()
            {
                IsSuccess = false,
                Message = "Failed to create cart",
                Value = null
            };
        }

        public async Task<InfrastructureResponse<bool>> DeleteCartAsync(int? userId, string? sessionId)
        {
            if (userId.HasValue)
            {
                const string sql = "DELETE FROM shopping_carts WHERE user_id = @UserId AND status = @Status";

                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                await using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("@UserId", userId.Value);
                command.Parameters.AddWithValue("@Status", CartStatus.Active.ToString());

                var affected = await command.ExecuteNonQueryAsync();

                return new InfrastructureResponse<bool>()
                {
                    IsSuccess = true,
                    Message = "Cart deleted successfully",
                    Value = affected > 0
                };
            }
            else if (!string.IsNullOrEmpty(sessionId))
            {
                const string sql = "DELETE FROM shopping_carts WHERE session_id = @SessionId AND status = @Status";

                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                await using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("@SessionId", sessionId);
                command.Parameters.AddWithValue("@Status", CartStatus.Active.ToString());

                var affected = await command.ExecuteNonQueryAsync();

                return new InfrastructureResponse<bool>()
                {
                    IsSuccess = true,
                    Message = "Cart deleted successfully",
                    Value = affected > 0
                };
            }

            return new InfrastructureResponse<bool>()
            {
                IsSuccess = false,
                Message = "Failed to delete cart",
                Value = false
            };
        }

        public async Task<InfrastructureResponse<Cart>> GetCartByUserIdAsync(int? userId, CartStatus? status = CartStatus.Active)
        {
            if (!userId.HasValue)
                return new InfrastructureResponse<Cart>()
                {
                    IsSuccess = false,
                    Message = "Invalid user ID",
                    Value = null
                };

            const string sql = @"
                SELECT
                    c.Id, c.UserId, c.SessionId, c.Status, c.CreatedAt, c.UpdatedAt, c.Version,
                    ci.Id AS Ci_Id, ci.ProductId AS Ci_ProductId, ci.Quantity AS Ci_Quantity, ci.UnitPrice AS Ci_UnitPrice,
                    ci.CreatedAt AS Ci_CreatedAt, ci.UpdatedAt AS Ci_UpdatedAt,
                    p.Id AS P_Id, p.Name AS P_Name, p.Description AS P_Description, p.StockQuantity AS P_StockQuantity,
                    p.ImageUrl AS P_ImageUrl, p.Price AS P_Price, p.ReservedStock AS P_ReservedStock,
                    p.LowStockThreshold AS P_LowStockThreshold, p.IsActive AS P_IsActive, p.SKU AS P_SKU,
                    p.CreatedAt AS P_CreatedAt, p.UpdatedAt AS P_UpdatedAt,
                    cc.Id AS Cc_Id, cc.CouponId AS Cc_CouponId, cc.DiscountAmount AS Cc_DiscountAmount,
                    cc.CreatedAt AS Cc_CreatedAt, cc.UpdatedAt AS Cc_UpdatedAt,
                    cpn.Id AS Cp_Id, cpn.Code AS Cp_Code, cpn.Description AS Cp_Description, cpn.DiscountAmount AS Cp_DiscountAmount,
                    cpn.DiscountValue AS Cp_MinimumOrderAmount, cpn.Type AS Cp_Type, cpn.Value AS Cp_Value,
                    cpn.IsActive AS Cp_IsActive, cpn.CreatedAt AS Cp_CreatedAt, cpn.UpdatedAt AS Cp_UpdatedAt
                FROM shopping_carts c
                LEFT JOIN cart_items ci ON c.Id = ci.ShoppingCartId
                LEFT JOIN products p ON ci.ProductId = p.Id
                LEFT JOIN cart_coupons cc ON c.Id = cc.CartId
                LEFT JOIN coupons cpn ON cc.CouponId = cpn.Id
                WHERE c.UserId = @UserId AND c.Status = @Status
                ORDER BY c.Id, ci.Id, cc.Id";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("UserId", userId.Value);
            command.Parameters.AddWithValue("Status", status?.ToString() ?? CartStatus.Active.ToString());

            await using var reader = await command.ExecuteReaderAsync();

            Cart? cart = null;
            var cartItems = new Dictionary<int, CartItem>();
            var cartCoupons = new Dictionary<int, CartCoupon>();

            while (await reader.ReadAsync())
            {
                int cartId = reader.GetInt32(reader.GetOrdinal("Id"));
                if (!cartItems.ContainsKey(cartId) || cart == null)
                {
                    cart = new Cart
                    {
                        Id = cartId,
                        UserId = reader.IsDBNull(reader.GetOrdinal("UserId")) ? null : reader.GetInt32(reader.GetOrdinal("UserId")),
                        SessionId = reader.IsDBNull(reader.GetOrdinal("SessionId")) ? null : reader.GetString(reader.GetOrdinal("SessionId")),
                        Status = Enum.Parse<CartStatus>(reader.GetString(reader.GetOrdinal("Status"))),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                        Version = reader.GetInt32(reader.GetOrdinal("Version")),
                        CartItems = new List<CartItem>(),
                        CartCoupons = new List<CartCoupon>()
                    };
                }

                if (!reader.IsDBNull(reader.GetOrdinal("Ci_Id")))
                {
                    var cartItemId = reader.GetInt32(reader.GetOrdinal("Ci_Id"));
                    if (!cartItems.TryGetValue(cartItemId, out var cartItem))
                    {
                        cartItem = new CartItem
                        {
                            Id = cartItemId,
                            ShoppingCartId = cartId,
                            ProductId = reader.GetInt32(reader.GetOrdinal("Ci_ProductId")),
                            Quantity = reader.GetInt32(reader.GetOrdinal("Ci_Quantity")),
                            UnitPrice = reader.GetDecimal(reader.GetOrdinal("Ci_UnitPrice")),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("Ci_CreatedAt")),
                            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("Ci_UpdatedAt"))
                        };
                        cartItems.Add(cartItemId, cartItem);
                        cart.CartItems.Add(cartItem);
                    }

 
                    if (!reader.IsDBNull(reader.GetOrdinal("P_Id")))
                    {
                        if (cartItem.Product == null)
                        {
                            cartItem.Product = new Product
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("P_Id")),
                                Name = reader.GetString(reader.GetOrdinal("P_Name")),
                                Description = reader.GetString(reader.GetOrdinal("P_Description")),
                                StockQuantity = reader.GetInt32(reader.GetOrdinal("P_StockQuantity")),
                                ImageUrl = reader.GetString(reader.GetOrdinal("P_ImageUrl")),
                                Price = reader.GetDecimal(reader.GetOrdinal("P_Price")),
                                ReservedStock = reader.GetInt32(reader.GetOrdinal("P_ReservedStock")),
                                LowStockThreshold = reader.GetInt32(reader.GetOrdinal("P_LowStockThreshold")),
                                IsActive = reader.GetBoolean(reader.GetOrdinal("P_IsActive")),
                                SKU = reader.GetString(reader.GetOrdinal("P_SKU")),
                                CreatedAt = reader.GetDateTime(reader.GetOrdinal("P_CreatedAt")),
                                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("P_UpdatedAt"))
                            };
                        }
                    }
                }

                if (!reader.IsDBNull(reader.GetOrdinal("Cc_Id")))
                {
                    var cartCouponId = reader.GetInt32(reader.GetOrdinal("Cc_Id"));
                    if (!cartCoupons.TryGetValue(cartCouponId, out var cartCoupon))
                    {
                        cartCoupon = new CartCoupon
                        {
                            Id = cartCouponId,
                            CartId = cartId,
                            CouponId = reader.GetInt32(reader.GetOrdinal("Cc_CouponId")),
                            DiscountAmount = reader.GetDecimal(reader.GetOrdinal("Cc_DiscountAmount")),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("Cc_CreatedAt")),
                            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("Cc_UpdatedAt"))
                        };
                        cartCoupons.Add(cartCouponId, cartCoupon);
                        cart.CartCoupons.Add(cartCoupon);
                    }

                    if (!reader.IsDBNull(reader.GetOrdinal("Cp_Id")))
                    {
                        if (cartCoupon.Coupon == null)
                        {
                            cartCoupon.Coupon = new Coupon
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Cp_Id")),
                                Code = reader.GetString(reader.GetOrdinal("Cp_Code")),
                                Description = reader.GetString(reader.GetOrdinal("Cp_Description")),
                                DiscountAmount = reader.GetDecimal(reader.GetOrdinal("Cp_DiscountAmount")),
                                MinimumOrderAmount = reader.GetDecimal(reader.GetOrdinal("Cp_MinimumOrderAmount")),
                                Type = (CouponType)reader.GetValue(reader.GetOrdinal("Cp_Type")),
                                Value = reader.GetDecimal(reader.GetOrdinal("Cp_Value")),
                                IsActive = reader.GetBoolean(reader.GetOrdinal("Cp_IsActive")),
                                CreatedAt = reader.GetDateTime(reader.GetOrdinal("Cp_CreatedAt")),
                                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("Cp_UpdatedAt")),
                                IsForNewUsersOnly = reader.GetBoolean(reader.GetOrdinal("Cp_IsForNewUsersOnly"))
                            };
                        }
                    }
                }
            }

            return new InfrastructureResponse<Cart>()
            {
                IsSuccess = true,
                Message = "Cart retrieved successfully",
                Value = cart
            };
        }

        public async Task<InfrastructureResponse<Cart>> GetCartBySessionIdAsync(string sessionId, CartStatus? status = CartStatus.Active)
        {
            if (string.IsNullOrEmpty(sessionId))
                return new InfrastructureResponse<Cart>()
                {
                    IsSuccess = false,
                    Message = "Invalid session ID",
                    Value = null
                };

            const string sql = @"
                SELECT
                    c.Id, c.UserId, c.SessionId, c.Status, c.CreatedAt, c.UpdatedAt, c.Version,
                    ci.Id AS Ci_Id, ci.ProductId AS Ci_ProductId, ci.Quantity AS Ci_Quantity, ci.UnitPrice AS Ci_UnitPrice,
                    ci.CreatedAt AS Ci_CreatedAt, ci.UpdatedAt AS Ci_UpdatedAt,
                    p.Id AS P_Id, p.Name AS P_Name, p.Description AS P_Description, p.StockQuantity AS P_StockQuantity,
                    p.ImageUrl AS P_ImageUrl, p.Price AS P_Price, p.ReservedStock AS P_ReservedStock,
                    p.LowStockThreshold AS P_LowStockThreshold, p.IsActive AS P_IsActive, p.SKU AS P_SKU,
                    p.CreatedAt AS P_CreatedAt, p.UpdatedAt AS P_UpdatedAt,
                    cc.Id AS Cc_Id, cc.CouponId AS Cc_CouponId, cc.DiscountAmount AS Cc_DiscountAmount,
                    cc.CreatedAt AS Cc_CreatedAt, cc.UpdatedAt AS Cc_UpdatedAt,
                    cpn.Id AS Cp_Id, cpn.Code AS Cp_Code, cpn.Description AS Cp_Description, cpn.DiscountType AS Cp_DiscountType,
                    cpn.DiscountAmount AS Cp_DiscountAmount, cpn.Type AS Cp_Type, cpn.Value AS Cp_Value,
                    cpn.IsActive AS Cp_IsActive, cpn.CreatedAt AS Cp_CreatedAt, cpn.UpdatedAt AS Cp_UpdatedAt
                FROM shopping_carts c
                LEFT JOIN cart_items ci ON c.Id = ci.ShoppingCartId
                LEFT JOIN products p ON ci.ProductId = p.Id
                LEFT JOIN cart_coupons cc ON c.Id = cc.CartId
                LEFT JOIN coupons cpn ON cc.CouponId = cpn.Id
                WHERE c.SessionId = @SessionId AND c.Status = @Status
                ORDER BY c.Id, ci.Id, cc.Id";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("SessionId", sessionId);
            command.Parameters.AddWithValue("Status", status?.ToString() ?? CartStatus.Active.ToString());

            await using var reader = await command.ExecuteReaderAsync();

            Cart? cart = null;
            var cartItems = new Dictionary<int, CartItem>();
            var cartCoupons = new Dictionary<int, CartCoupon>();

            while (await reader.ReadAsync())
            {
                int cartId = reader.GetInt32(reader.GetOrdinal("Id"));
                if (!cartItems.ContainsKey(cartId) || cart == null)
                {
                    cart = new Cart
                    {
                        Id = cartId,
                        UserId = reader.IsDBNull(reader.GetOrdinal("UserId")) ? null : reader.GetInt32(reader.GetOrdinal("UserId")),
                        SessionId = reader.IsDBNull(reader.GetOrdinal("SessionId")) ? null : reader.GetString(reader.GetOrdinal("SessionId")),
                        Status = Enum.Parse<CartStatus>(reader.GetString(reader.GetOrdinal("Status"))),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                        Version = reader.GetInt32(reader.GetOrdinal("Version")),
                        CartItems = new List<CartItem>(),
                        CartCoupons = new List<CartCoupon>()
                    };
                }

                if (!reader.IsDBNull(reader.GetOrdinal("Ci_Id")))
                {
                    var cartItemId = reader.GetInt32(reader.GetOrdinal("Ci_Id"));
                    if (!cartItems.TryGetValue(cartItemId, out var cartItem))
                    {
                        cartItem = new CartItem
                        {
                            Id = cartItemId,
                            ShoppingCartId = cartId,
                            ProductId = reader.GetInt32(reader.GetOrdinal("Ci_ProductId")),
                            Quantity = reader.GetInt32(reader.GetOrdinal("Ci_Quantity")),
                            UnitPrice = reader.GetDecimal(reader.GetOrdinal("Ci_UnitPrice")),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("Ci_CreatedAt")),
                            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("Ci_UpdatedAt"))
                        };
                        cartItems.Add(cartItemId, cartItem);
                        cart.CartItems.Add(cartItem);
                    }

                    if (!reader.IsDBNull(reader.GetOrdinal("P_Id")))
                    {
                        if (cartItem.Product == null)
                        {
                            cartItem.Product = new Product
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("P_Id")),
                                Name = reader.GetString(reader.GetOrdinal("P_Name")),
                                Description = reader.GetString(reader.GetOrdinal("P_Description")),
                                StockQuantity = reader.GetInt32(reader.GetOrdinal("P_StockQuantity")),
                                ImageUrl = reader.GetString(reader.GetOrdinal("P_ImageUrl")),
                                Price = reader.GetDecimal(reader.GetOrdinal("P_Price")),
                                ReservedStock = reader.GetInt32(reader.GetOrdinal("P_ReservedStock")),
                                LowStockThreshold = reader.GetInt32(reader.GetOrdinal("P_LowStockThreshold")),
                                IsActive = reader.GetBoolean(reader.GetOrdinal("P_IsActive")),
                                SKU = reader.GetString(reader.GetOrdinal("P_SKU")),
                                CreatedAt = reader.GetDateTime(reader.GetOrdinal("P_CreatedAt")),
                                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("P_UpdatedAt"))
                            };
                        }
                    }
                }

                if (!reader.IsDBNull(reader.GetOrdinal("Cc_Id")))
                {
                    var cartCouponId = reader.GetInt32(reader.GetOrdinal("Cc_Id"));
                    if (!cartCoupons.TryGetValue(cartCouponId, out var cartCoupon))
                    {
                        cartCoupon = new CartCoupon
                        {
                            Id = cartCouponId,
                            CartId = cartId,
                            CouponId = reader.GetInt32(reader.GetOrdinal("Cc_CouponId")),
                            DiscountAmount = reader.GetDecimal(reader.GetOrdinal("Cc_DiscountAmount")),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("Cc_CreatedAt")),
                            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("Cc_UpdatedAt"))
                        };
                        cartCoupons.Add(cartCouponId, cartCoupon);
                        cart.CartCoupons.Add(cartCoupon);
                    }

                    if (!reader.IsDBNull(reader.GetOrdinal("Cp_Id")))
                    {
                        if (cartCoupon.Coupon == null)
                        {
                            cartCoupon.Coupon = new Coupon
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Cp_Id")),
                                Code = reader.GetString(reader.GetOrdinal("Cp_Code")),
                                Description = reader.GetString(reader.GetOrdinal("Cp_Description")),
                                DiscountAmount = reader.GetDecimal(reader.GetOrdinal("Cp_DiscountAmount")),
                                Value = reader.GetDecimal(reader.GetOrdinal("Cp_Value")),
                                Type = (CouponType)reader.GetValue(reader.GetOrdinal("Cp_Type")),
                                IsForNewUsersOnly = reader.GetBoolean(reader.GetOrdinal("Cp_IsForNewUsersOnly")),
                                IsActive = reader.GetBoolean(reader.GetOrdinal("Cp_IsActive")),
                                CreatedAt = reader.GetDateTime(reader.GetOrdinal("Cp_CreatedAt")),
                                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("Cp_UpdatedAt"))
                            };
                        }
                    }
                }
            }

            return new InfrastructureResponse<Cart>()
            {
                IsSuccess = true,
                Message = "Cart retrieved successfully",
                Value = cart
            };
        }

        public async Task<InfrastructureResponse<List<Cart>>> GetCartsAsync()
        {
            const string sql = @"
                SELECT
                    c.id, c.user_id, c.session_id, c.status, c.created_at, c.updated_at, c.version
                FROM shopping_carts c
                ORDER BY c.id";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);

            await using var reader = await command.ExecuteReaderAsync();

            var carts = new List<Cart>();

            while (await reader.ReadAsync())
            {
                var cart = MapCart(reader);

                carts.Add(cart);
            }

            return new InfrastructureResponse<List<Cart>>()
            {
                IsSuccess = true,
                Message = "Carts retrieved successfully",
                Value = carts
            };
        }

        public async Task<InfrastructureResponse<Cart>> UpdateCartAsync(Cart cart)
        {
            const string sql = @"
                UPDATE shopping_carts
                SET UserId = @UserId,
                    SessionId = @SessionId,
                    Status = @Status,
                    UpdatedAt = @UpdatedAt,
                    Version = Version + 1
                WHERE Id = @Id
                RETURNING Id, UserId, SessionId, Status, CreatedAt, UpdatedAt, Version";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("UserId", (object?)cart.UserId ?? DBNull.Value);
            command.Parameters.AddWithValue("SessionId", (object?)cart.SessionId ?? DBNull.Value);
            command.Parameters.AddWithValue("Status", cart.Status.ToString());
            command.Parameters.AddWithValue("UpdatedAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("Id", cart.Id);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                cart.Id = reader.GetInt32(reader.GetOrdinal("Id"));
                cart.UserId = reader.IsDBNull(reader.GetOrdinal("UserId")) ? null : reader.GetInt32(reader.GetOrdinal("UserId"));
                cart.SessionId = reader.IsDBNull(reader.GetOrdinal("SessionId")) ? null : reader.GetString(reader.GetOrdinal("SessionId"));
                cart.Status = Enum.Parse<CartStatus>(reader.GetString(reader.GetOrdinal("Status")));
                cart.CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"));
                cart.UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"));
                cart.Version = reader.GetInt32(reader.GetOrdinal("Version"));
                return new InfrastructureResponse<Cart>()
                {
                    IsSuccess = true,
                    Message = "Cart updated successfully",
                    Value = cart
                };
            }

            return new InfrastructureResponse<Cart>()
            {
                IsSuccess = false,
                Message = "Cart not found",
                Value = null
            };
        }

        public async Task<InfrastructureResponse<bool>> DeleteCartById(int Id)
        {
            if (Id <= 0)
                return new InfrastructureResponse<bool>()
                {
                    IsSuccess = false,
                    Message = "Invalid cart ID",
                    Value = false
                };

            const string sql = "DELETE FROM shopping_carts WHERE Id = @Id AND Status = @Status";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("Id", Id);
            command.Parameters.AddWithValue("Status", CartStatus.Active.ToString());

            var affected = await command.ExecuteNonQueryAsync();
            return new InfrastructureResponse<bool>()
            {
                IsSuccess = true,
                Message = affected > 0 ? "Cart deleted successfully" : "Cart not found",
                Value = affected > 0
            };
        }

        public async Task<InfrastructureResponse<Cart>> UpdateCartStatusAsync(int? userId, CartStatus status)
        {
            if (!userId.HasValue)
                return new InfrastructureResponse<Cart>()
                {
                    IsSuccess = false,
                    Message = "Invalid user ID",
                    Value = null
                };

            const string sql = @"
                UPDATE shopping_carts
                SET status = @Status,
                    updated_at = @UpdatedAt,
                    version = version + 1
                WHERE user_id = @UserId
                RETURNING id, user_id, session_id, status, created_at, updated_at, version";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Status", status.ToString());
            command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("@UserId", userId.Value);

            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var cart = MapCart(reader);

                return new InfrastructureResponse<Cart>()
                {
                    IsSuccess = true,
                    Message = "Cart updated successfully",
                    Value = cart
                };
            }

            return new InfrastructureResponse<Cart>()
            {
                IsSuccess = false,
                Message = "Cart not found",
                Value = null
            };
        }

        public async Task<InfrastructureResponse<Cart>> GetCartByIdAsync(int cartId, CartStatus status)
        {
            if (cartId <= 0)
                return new InfrastructureResponse<Cart>()
                {
                    IsSuccess = false,
                    Message = "Invalid cart ID",
                    Value = null
                };

            const string sql = @"
                SELECT
                    c.Id, c.UserId, c.SessionId, c.Status, c.CreatedAt, c.UpdatedAt, c.Version,
                    ci.Id AS Ci_Id, ci.ProductId AS Ci_ProductId, ci.Quantity AS Ci_Quantity, ci.UnitPrice AS Ci_UnitPrice,
                    ci.CreatedAt AS Ci_CreatedAt, ci.UpdatedAt AS Ci_UpdatedAt,
                    p.Id AS P_Id, p.Name AS P_Name, p.Description AS P_Description, p.StockQuantity AS P_StockQuantity,
                    p.ImageUrl AS P_ImageUrl, p.Price AS P_Price, p.ReservedStock AS P_ReservedStock,
                    p.LowStockThreshold AS P_LowStockThreshold, p.IsActive AS P_IsActive, p.SKU AS P_SKU,
                    p.CreatedAt AS P_CreatedAt, p.UpdatedAt AS P_UpdatedAt,
                    cc.Id AS Cc_Id, cc.CouponId AS Cc_CouponId, cc.DiscountAmount AS Cc_DiscountAmount,
                    cc.CreatedAt AS Cc_CreatedAt, cc.UpdatedAt AS Cc_UpdatedAt,
                    cpn.Id AS Cp_Id, cpn.Code AS Cp_Code, cpn.Description AS Cp_Description, cpn.DiscountType AS Cp_DiscountType,
                    cpn.DiscountValue AS Cp_DiscountValue, cpn.StartDate AS Cp_StartDate, cpn.EndDate AS Cp_EndDate,
                    cpn.IsActive AS Cp_IsActive, cpn.CreatedAt AS Cp_CreatedAt, cpn.UpdatedAt AS Cp_UpdatedAt
                FROM shopping_carts c
                LEFT JOIN cart_items ci ON c.Id = ci.ShoppingCartId
                LEFT JOIN products p ON ci.ProductId = p.Id
                LEFT JOIN cart_coupons cc ON c.Id = cc.CartId
                LEFT JOIN coupons cpn ON cc.CouponId = cpn.Id
                WHERE c.Id = @CartId AND c.Status = @Status
                ORDER BY c.Id, ci.Id, cc.Id";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("CartId", cartId);
            command.Parameters.AddWithValue("Status", status.ToString());

            await using var reader = await command.ExecuteReaderAsync();

            Cart? cart = null;
            var cartItems = new Dictionary<int, CartItem>();
            var cartCoupons = new Dictionary<int, CartCoupon>();

            while (await reader.ReadAsync())
            {
                int cartIdResult = reader.GetInt32(reader.GetOrdinal("Id"));
                if (!cartItems.ContainsKey(cartIdResult) || cart == null)
                {
                    cart = new Cart
                    {
                        Id = cartIdResult,
                        UserId = reader.IsDBNull(reader.GetOrdinal("UserId")) ? null : reader.GetInt32(reader.GetOrdinal("UserId")),
                        SessionId = reader.IsDBNull(reader.GetOrdinal("SessionId")) ? null : reader.GetString(reader.GetOrdinal("SessionId")),
                        Status = Enum.Parse<CartStatus>(reader.GetString(reader.GetOrdinal("Status"))),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                        Version = reader.GetInt32(reader.GetOrdinal("Version")),
                        CartItems = new List<CartItem>(),
                        CartCoupons = new List<CartCoupon>()
                    };
                }

                if (!reader.IsDBNull(reader.GetOrdinal("Ci_Id")))
                {
                    var cartItemId = reader.GetInt32(reader.GetOrdinal("Ci_Id"));
                    if (!cartItems.TryGetValue(cartItemId, out var cartItem))
                    {
                        cartItem = new CartItem
                        {
                            Id = cartItemId,
                            ShoppingCartId = cartIdResult,
                            ProductId = reader.GetInt32(reader.GetOrdinal("Ci_ProductId")),
                            Quantity = reader.GetInt32(reader.GetOrdinal("Ci_Quantity")),
                            UnitPrice = reader.GetDecimal(reader.GetOrdinal("Ci_UnitPrice")),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("Ci_CreatedAt")),
                            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("Ci_UpdatedAt"))
                        };
                        cartItems.Add(cartItemId, cartItem);
                        cart.CartItems.Add(cartItem);
                    }


                    if (!reader.IsDBNull(reader.GetOrdinal("P_Id")))
                    {
                        if (cartItem.Product == null)
                        {
                            cartItem.Product = new Product
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("P_Id")),
                                Name = reader.GetString(reader.GetOrdinal("P_Name")),
                                Description = reader.GetString(reader.GetOrdinal("P_Description")),
                                StockQuantity = reader.GetInt32(reader.GetOrdinal("P_StockQuantity")),
                                ImageUrl = reader.GetString(reader.GetOrdinal("P_ImageUrl")),
                                Price = reader.GetDecimal(reader.GetOrdinal("P_Price")),
                                ReservedStock = reader.GetInt32(reader.GetOrdinal("P_ReservedStock")),
                                LowStockThreshold = reader.GetInt32(reader.GetOrdinal("P_LowStockThreshold")),
                                IsActive = reader.GetBoolean(reader.GetOrdinal("P_IsActive")),
                                SKU = reader.GetString(reader.GetOrdinal("P_SKU")),
                                CreatedAt = reader.GetDateTime(reader.GetOrdinal("P_CreatedAt")),
                                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("P_UpdatedAt"))
                            };
                        }
                    }
                }

                if (!reader.IsDBNull(reader.GetOrdinal("Cc_Id")))
                {
                    var cartCouponId = reader.GetInt32(reader.GetOrdinal("Cc_Id"));
                    if (!cartCoupons.TryGetValue(cartCouponId, out var cartCoupon))
                    {
                        cartCoupon = new CartCoupon
                        {
                            Id = cartCouponId,
                            CartId = cartIdResult,
                            CouponId = reader.GetInt32(reader.GetOrdinal("Cc_CouponId")),
                            DiscountAmount = reader.GetDecimal(reader.GetOrdinal("Cc_DiscountAmount")),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("Cc_CreatedAt")),
                            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("Cc_UpdatedAt"))
                        };
                        cartCoupons.Add(cartCouponId, cartCoupon);
                        cart.CartCoupons.Add(cartCoupon);
                    }

                    if (!reader.IsDBNull(reader.GetOrdinal("Cp_Id")))
                    {
                        if (cartCoupon.Coupon == null)
                        {
                            cartCoupon.Coupon = new Coupon
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Cp_Id")),
                                Code = reader.GetString(reader.GetOrdinal("Cp_Code")),
                                Description = reader.GetString(reader.GetOrdinal("Cp_Description")),
                                Value = reader.GetDecimal(reader.GetOrdinal("Cp_Value")),
                                Type = (CouponType)reader.GetValue(reader.GetOrdinal("Cp_Type")),
                                DiscountAmount = reader.GetDecimal(reader.GetOrdinal("Cp_DiscountAmount")),
                                IsForNewUsersOnly = reader.GetBoolean(reader.GetOrdinal("Cp_IsForNewUsersOnly")),
                                IsActive = reader.GetBoolean(reader.GetOrdinal("Cp_IsActive")),
                                CreatedAt = reader.GetDateTime(reader.GetOrdinal("Cp_CreatedAt")),
                                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("Cp_UpdatedAt"))
                            };
                        }
                    }
                }
            }

            return new InfrastructureResponse<Cart>()
            {
                IsSuccess = true,
                Message = "Cart retrieved successfully",
                Value = cart
            };
        }

        public async Task<InfrastructureResponse<Cart>> UpdateItemQuantityAsync(int? userId, string? sessionId, int productId, int quantity)
        {
            Cart? cart = null;
            if (userId.HasValue)
            {
                cart =  GetCartByUserIdAsync(userId, CartStatus.Active).GetAwaiter().GetResult().Value;
            }
            else if (!string.IsNullOrEmpty(sessionId))
            {
                cart = GetCartBySessionIdAsync(sessionId, CartStatus.Active).GetAwaiter().GetResult().Value;
            }

            if (cart == null)
                return new InfrastructureResponse<Cart>()
                {
                    IsSuccess = false,
                    Message = "Cart not found",
                    Value = null
                };

            var cartItem = cart.CartItems.FirstOrDefault(i => i.ProductId == productId);
            if (cartItem == null)
                return new InfrastructureResponse<Cart>()
                {
                    IsSuccess = false,
                    Message = "Product not found in cart",
                    Value = null
                };

            cartItem.Quantity = quantity;
            cartItem.UpdatedAt = DateTime.UtcNow;

            const string updateItemSql = @"
                UPDATE cart_items
                SET quantity = @Quantity,
                    updated_at = @UpdatedAt
                WHERE id = @Id
                RETURNING id, quantity, updated_at";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(updateItemSql, connection);
            command.Parameters.AddWithValue("@Quantity", cartItem.Quantity);
            command.Parameters.AddWithValue("@UpdatedAt", cartItem.UpdatedAt);
            command.Parameters.AddWithValue("@Id", cartItem.Id);

            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                cartItem.Quantity = reader.GetInt32(reader.GetOrdinal("quantity"));
                cartItem.UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"));

                return new InfrastructureResponse<Cart>()
                {
                    IsSuccess = true,
                    Message = "Item quantity updated successfully",
                    Value = cart
                };
            }

            return new InfrastructureResponse<Cart>()
            {
                IsSuccess = false,
                Message = "Failed to update item quantity",
                Value = null
            };
        }

        public async Task<InfrastructureResponse<CartItem>> CreateItemAsync(int cartId, int? userId, string? sessionId, int productId, int quantity, decimal unitPrice)
        {
            Cart? cart = null;
            if (userId.HasValue)
            {
                cart = GetCartByUserIdAsync(userId, CartStatus.Active).GetAwaiter().GetResult().Value;
            }
            else if (!string.IsNullOrEmpty(sessionId))
            {
                cart = GetCartBySessionIdAsync(sessionId, CartStatus.Active).GetAwaiter().GetResult().Value;
            }

            if (cart == null || cart.Id != cartId)
                return new InfrastructureResponse<CartItem>()
                {
                    IsSuccess = false,
                    Message = "Cart not found",
                    Value = null
                };

            const string sql = @"
                INSERT INTO cart_items (cart_id, product_id, user_id, session_id, quantity, unit_price, created_at, updated_at)
                VALUES (@ShoppingCartId, @ProductId, @UserId, @SessionId, @Quantity, @UnitPrice, @CreatedAt, @UpdatedAt)
                RETURNING id, cart_id, product_id, user_id, session_id, quantity, unit_price, created_at, updated_at";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ShoppingCartId", cartId);
            command.Parameters.AddWithValue("@ProductId", productId);
            command.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
            command.Parameters.AddWithValue("@SessionId", (object?)sessionId ?? DBNull.Value);
            command.Parameters.AddWithValue("@Quantity", quantity);
            command.Parameters.AddWithValue("@UnitPrice", unitPrice);
            command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);

            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var cartItem = new CartItem
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    ShoppingCartId = reader.GetInt32(reader.GetOrdinal("cart_id")),
                    ProductId = reader.GetInt32(reader.GetOrdinal("product_id")),
                    UserId = reader.IsDBNull(reader.GetOrdinal("user_id")) ? null : reader.GetInt32(reader.GetOrdinal("user_id")),
                    SessionId = reader.IsDBNull(reader.GetOrdinal("session_id")) ? null : reader.GetString(reader.GetOrdinal("session_id")),
                    Quantity = reader.GetInt32(reader.GetOrdinal("quantity")),
                    UnitPrice = reader.GetDecimal(reader.GetOrdinal("unit_price")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
                };
                return new InfrastructureResponse<CartItem>()
                {
                    IsSuccess = true,
                    Message = "Item found",
                    Value = cartItem
                };
            }

            return new InfrastructureResponse<CartItem>()
            {
                IsSuccess = false,
                Message = "Item not found",
                Value = null
            };
        }

        public async Task<InfrastructureResponse<bool>> RemoveItemAsync(int? userId, string? sessionId, int productId)
        {
            Cart? cart = null;
            if (userId.HasValue)
            {
                cart = GetCartByUserIdAsync(userId, CartStatus.Active).GetAwaiter().GetResult().Value;
            }
            else if (!string.IsNullOrEmpty(sessionId))
            {
                cart = GetCartBySessionIdAsync(sessionId, CartStatus.Active).GetAwaiter().GetResult().Value;
            }

            if (cart == null)
                return new InfrastructureResponse<bool>()
                {
                    IsSuccess = false,
                    Message = "Cart not found",
                    Value = false
                };

            var cartItem = cart.CartItems.FirstOrDefault(i => i.ProductId == productId);

            if (cartItem == null)
                return new InfrastructureResponse<bool>()
                {
                    IsSuccess = false,
                    Message = "Item not found",
                    Value = false
                };

            const string sql = "DELETE FROM cart_items WHERE id = @Id";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", cartItem.Id);

            var affected = await command.ExecuteNonQueryAsync();
            return new InfrastructureResponse<bool>()
            {
                IsSuccess = true,
                Message = "Item removed successfully",
                Value = affected > 0
            };
        }

        public async Task<InfrastructureResponse<bool>> UpdateItemsAsync(List<CartItem> items)
        {
            if (items == null || items.Count == 0)
                return new InfrastructureResponse<bool>()
                {
                    IsSuccess = true,
                    Message = "No items to update",
                    Value = true
                };

            foreach (var item in items)
            {
                const string sql = @"
                    UPDATE cart_items
                    SET quantity = @Quantity,
                        unit_price = @UnitPrice,
                        updated_at = @UpdatedAt
                    WHERE id = @Id";

                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                await using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Quantity", item.Quantity);
                command.Parameters.AddWithValue("@UnitPrice", item.UnitPrice);
                command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
                command.Parameters.AddWithValue("@Id", item.Id);

                var affected = await command.ExecuteNonQueryAsync();

                if (affected <= 0)
                    return new InfrastructureResponse<bool>()
                    {
                        IsSuccess = false,
                        Message = "Failed to update item",
                        Value = false
                    };
            }

            return new InfrastructureResponse<bool>()
            {
                IsSuccess = true,
                Message = "Items updated successfully",
                Value = true
            };
        }

        public async Task<InfrastructureResponse<bool>> IsItemExistingByRealCart(int cartId, int productId, int userId)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM cart_items ci
                JOIN shopping_carts c ON ci.cart_id = c.id
                WHERE ci.cart_id != @CartId
                  AND ci.product_id = @ProductId
                  AND ci.user_id = @UserId";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@CartId", cartId);
            command.Parameters.AddWithValue("@ProductId", productId);
            command.Parameters.AddWithValue("@UserId", userId);

            var count = await command.ExecuteScalarAsync();

            return new InfrastructureResponse<bool>()
            {
                IsSuccess = true,
                Message = "Item existence check completed",
                Value = count != null && Convert.ToInt32(count) > 0
            };
        }

        public async Task<InfrastructureResponse<bool>> RemoveItemAsync(int cartId, int userId, int productId)
        {
            const string getCartSql = @"
                SELECT id
                FROM shopping_carts
                WHERE user_id = @UserId AND status = @Status";

            Cart? cart = null;
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(getCartSql, connection);
            command.Parameters.AddWithValue("UserId", userId);
            command.Parameters.AddWithValue("Status", CartStatus.Active.ToString());

            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                cart = new Cart
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    UserId = userId,
                    CartItems = new List<CartItem>()
                };
            }

            if (cart == null || cart.Id != cartId)
                return new InfrastructureResponse<bool>()
                {
                    IsSuccess = false,
                    Message = "Cart not found or invalid",
                    Value = false
                };

            const string getItemSql = @"
                SELECT id
                FROM cart_items
                WHERE cart_id = @CartId AND product_id = @ProductId AND user_id = @UserId";

            await using var command2 = new NpgsqlCommand(getItemSql, connection);
            command2.Parameters.AddWithValue("@CartId", cartId);
            command2.Parameters.AddWithValue("@ProductId", productId);
            command2.Parameters.AddWithValue("@UserId", userId);

            var itemIdObj = await command2.ExecuteScalarAsync();

            if (itemIdObj == null)
                return new InfrastructureResponse<bool>()
                {
                    IsSuccess = false,
                    Message = "Item not found",
                    Value = false
                };

            int itemId = Convert.ToInt32(itemIdObj);

            const string deleteSql = "DELETE FROM cart_items WHERE id = @Id";

            await using var command3 = new NpgsqlCommand(deleteSql, connection);
            command3.Parameters.AddWithValue("@Id", itemId);

            var affected = await command3.ExecuteNonQueryAsync();
            return new InfrastructureResponse<bool>()
            {
                IsSuccess = true,
                Message = "Item removed successfully",
                Value = affected > 0
            };
        }

        public async Task<InfrastructureResponse<bool>> IsConverted(int cartId)
        {
            const string sql = "SELECT COUNT(1) FROM shopping_carts WHERE id = @Id AND status = @Status";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", cartId);
            command.Parameters.AddWithValue("@Status", CartStatus.Converted.ToString());

            var count = await command.ExecuteScalarAsync();
            return new InfrastructureResponse<bool>()
            {
                IsSuccess = true,
                Message = "Conversion status check completed",
                Value = count != null && Convert.ToInt32(count) > 0
            };
        }


        private Cart MapCart(NpgsqlDataReader reader)
        {
            return new Cart()
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                UserId = reader.IsDBNull(reader.GetOrdinal("user_id")) ? null : reader.GetInt32(reader.GetOrdinal("user_id")),
                SessionId = reader.IsDBNull(reader.GetOrdinal("session_id")) ? null : reader.GetString(reader.GetOrdinal("session_id")),
                Status = Enum.Parse<CartStatus>(reader.GetString(reader.GetOrdinal("status"))),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at")),
                Version = reader.GetInt32(reader.GetOrdinal("version"))
            };
        }
    }
}