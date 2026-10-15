using Berryfy.Domain.Constants;
using Berryfy.Domain.Entities;
using Berryfy.Domain.Entities.AuthEntities;
using Berryfy.Domain.Entities.CouponEntities;
using Berryfy.Domain.Entities.ProductEntities;
using Berryfy.Domain.Entities.ShoppingCartEntities;
using Berryfy.Domain.Repositories.ShoppingCartInterfaces;
using Berryfy.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;

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
                INSERT INTO shopping_carts (user_id, session_id, status, created_at, updated_at, version, notes)
                VALUES (@UserId, @SessionId, @Status, @CreatedAt, @UpdatedAt, @Version, @Notes)
                RETURNING id, user_id, session_id, status, created_at, updated_at, version, notes;";

            using var connection = await OpenConnectionAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
            command.Parameters.AddWithValue("@SessionId", (object?)null ?? DBNull.Value);
            command.Parameters.AddWithValue("@Status", status.ToString());
            command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("@Version", 1);
            command.Parameters.AddWithValue("@Notes", "initial cart");

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
                INSERT INTO shopping_carts (user_id, session_id, status, created_at, updated_at, version, notes)
                VALUES (@UserId, @SessionId, @Status, @CreatedAt, @UpdatedAt, @Version, @Notes)
                RETURNING id, user_id, session_id, status, created_at, updated_at, version, notes";

            using var connection = await OpenConnectionAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue(@"UserId", (object?)null ?? DBNull.Value);
            command.Parameters.AddWithValue("@SessionId", (object?)sessionId ?? DBNull.Value);
            command.Parameters.AddWithValue("@Status", status.ToString());
            command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("@Version", 1);
            command.Parameters.AddWithValue("@Notes", "initical cart"); 

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

                using var connection = await OpenConnectionAsync();

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


        public async Task<InfrastructureResponse<Cart>> GetCartByUserIdAsync(
            int? userId,
            CartStatus? status = CartStatus.Active)
        {
            if (!userId.HasValue)
            {
                return new InfrastructureResponse<Cart>
                {
                    IsSuccess = false,
                    Message = "Invalid user ID",
                    Value = null
                };
            }

            const string sql = @"
        SELECT
            c.id AS Id,
            c.user_id AS UserId,
            c.session_id AS SessionId,
            c.status AS Status,
            c.created_at AS CreatedAt,
            c.updated_at AS UpdatedAt,
            c.version AS Version,
            c.notes AS Notes,

            ci.id AS Ci_Id,
            ci.product_id AS Ci_ProductId,
            ci.quantity AS Ci_Quantity,
            ci.unit_price AS Ci_UnitPrice,
            ci.created_at AS Ci_CreatedAt,
            ci.updated_at AS Ci_UpdatedAt,

            p.id AS P_Id,
            p.name AS P_Name,
            p.description AS P_Description,
            p.stock_quantity AS P_StockQuantity,
            p.image_url AS P_ImageUrl,
            p.price AS P_Price,
            p.reserved_stock AS P_ReservedStock,
            p.low_stock_threshold AS P_LowStockThreshold,
            p.is_active AS P_IsActive,
            p.sku AS P_SKU,
            p.created_at AS P_CreatedAt,
            p.updated_at AS P_UpdatedAt,

            cc.id AS Cc_Id,
            cc.coupon_id AS Cc_CouponId,
            cc.discount_amount AS Cc_DiscountAmount,
            cc.created_at AS Cc_CreatedAt,
            cc.updated_at AS Cc_UpdatedAt,
            cc.applied_at AS Cc_AppliedAt,

            cpn.id AS Cp_Id,
            cpn.code AS Cp_Code,
            cpn.description AS Cp_Description,
            cpn.discount_amount AS Cp_DiscountAmount,
            cpn.minimum_order_amount AS Cp_MinimumOrderAmount,
            cpn.type AS Cp_Type,
            cpn.value AS Cp_Value,
            cpn.is_active AS Cp_IsActive,
            cpn.is_for_new_users_only AS Cp_IsForNewUsersOnly,
            cpn.created_at AS Cp_CreatedAt,
            cpn.updated_at AS Cp_UpdatedAt

        FROM shopping_carts c
        LEFT JOIN cart_items ci
            ON c.id = ci.cart_id
        LEFT JOIN products p
            ON ci.product_id = p.id
        LEFT JOIN cart_coupons cc
            ON c.id = cc.cart_id
        LEFT JOIN coupons cpn
            ON cc.coupon_id = cpn.id

        WHERE c.id = (
            SELECT c2.id
            FROM shopping_carts c2
            WHERE c2.user_id = @UserId
              AND c2.status = @Status
            ORDER BY c2.id
            LIMIT 1
        )

        ORDER BY ci.id, cc.id";

            using var connection = await OpenConnectionAsync();

            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@UserId", userId.Value);
            command.Parameters.AddWithValue(
                "@Status",
                (status ?? CartStatus.Active).ToString());

            await using var reader = await command.ExecuteReaderAsync();

            Cart? cart = null;

            var cartItems = new Dictionary<int, CartItem>();
            var cartCoupons = new Dictionary<int, CartCoupon>();

            int Ordinal(string name) => reader.GetOrdinal(name);

            string? NullableString(string name) =>
                reader.IsDBNull(Ordinal(name))
                    ? null
                    : reader.GetString(Ordinal(name));

            while (await reader.ReadAsync())
            {
                int cartId = reader.GetInt32(Ordinal("Id"));

                if (cart == null)
                {
                    cart = new Cart
                    {
                        Id = cartId,
                        UserId = reader.IsDBNull(Ordinal("UserId"))
                            ? null
                            : reader.GetInt32(Ordinal("UserId")),
                        SessionId = NullableString("SessionId"),
                        Status = Enum.Parse<CartStatus>(
                            reader.GetString(Ordinal("Status"))),
                        CreatedAt = reader.GetDateTime(Ordinal("CreatedAt")),
                        UpdatedAt = reader.GetDateTime(Ordinal("UpdatedAt")),
                        Version = reader.GetInt32(Ordinal("Version")),
                        Note = NullableString("Notes"),
                        CartItems = new List<CartItem>(),
                        CartCoupons = new List<CartCoupon>()
                    };
                }

                // Map cart items.
                if (!reader.IsDBNull(Ordinal("Ci_Id")))
                {
                    int cartItemId = reader.GetInt32(Ordinal("Ci_Id"));

                    if (!cartItems.TryGetValue(cartItemId, out var cartItem))
                    {
                        cartItem = new CartItem
                        {
                            Id = cartItemId,
                            ShoppingCartId = cartId,
                            ProductId = reader.GetInt32(Ordinal("Ci_ProductId")),
                            Quantity = reader.GetInt32(Ordinal("Ci_Quantity")),
                            UnitPrice = reader.GetDecimal(Ordinal("Ci_UnitPrice")),
                            CreatedAt = reader.GetDateTime(Ordinal("Ci_CreatedAt")),
                            UpdatedAt = reader.GetDateTime(Ordinal("Ci_UpdatedAt"))
                        };

                        cartItems.Add(cartItemId, cartItem);
                        cart.CartItems.Add(cartItem);
                    }

                    // Map the associated product once per cart item.
                    if (cartItem.Product == null &&
                        !reader.IsDBNull(Ordinal("P_Id")))
                    {
                        cartItem.Product = new Product
                        {
                            Id = reader.GetInt32(Ordinal("P_Id")),
                            Name = reader.GetString(Ordinal("P_Name")),
                            Description = NullableString("P_Description"),
                            StockQuantity = reader.GetInt32(Ordinal("P_StockQuantity")),
                            ImageUrl = NullableString("P_ImageUrl"),
                            Price = reader.GetDecimal(Ordinal("P_Price")),
                            ReservedStock = reader.GetInt32(Ordinal("P_ReservedStock")),
                            LowStockThreshold = reader.GetInt32(Ordinal("P_LowStockThreshold")),
                            IsActive = reader.GetBoolean(Ordinal("P_IsActive")),
                            SKU = NullableString("P_SKU"),
                            CreatedAt = reader.GetDateTime(Ordinal("P_CreatedAt")),
                            UpdatedAt = reader.GetDateTime(Ordinal("P_UpdatedAt"))
                        };
                    }
                }

                // Map applied coupons.
                if (!reader.IsDBNull(Ordinal("Cc_Id")))
                {
                    int cartCouponId = reader.GetInt32(Ordinal("Cc_Id"));

                    if (!cartCoupons.TryGetValue(cartCouponId, out var cartCoupon))
                    {
                        cartCoupon = new CartCoupon
                        {
                            Id = cartCouponId,
                            CartId = cartId,
                            CouponId = reader.GetInt32(Ordinal("Cc_CouponId")),
                            DiscountAmount = reader.GetDecimal(Ordinal("Cc_DiscountAmount")),
                            CreatedAt = reader.GetDateTime(Ordinal("Cc_CreatedAt")),
                            UpdatedAt = reader.GetDateTime(Ordinal("Cc_UpdatedAt")),
                            AppliedAt = reader.GetDateTime(Ordinal("Cc_AppliedAt"))
                        };

                        cartCoupons.Add(cartCouponId, cartCoupon);
                        cart.CartCoupons.Add(cartCoupon);
                    }

                    if (cartCoupon.Coupon == null &&
                        !reader.IsDBNull(Ordinal("Cp_Id")))
                    {
                        cartCoupon.Coupon = new Coupon
                        {
                            Id = reader.GetInt32(Ordinal("Cp_Id")),
                            Code = reader.GetString(Ordinal("Cp_Code")),
                            Description = NullableString("Cp_Description"),
                            DiscountAmount = reader.GetDecimal(Ordinal("Cp_DiscountAmount")),
                            MinimumOrderAmount = reader.GetDecimal(Ordinal("Cp_MinimumOrderAmount")),
                            Type = 
                                (CouponType)reader.GetInt16(Ordinal("Cp_Type")),
                            Value = reader.GetDecimal(Ordinal("Cp_Value")),
                            IsActive = reader.GetBoolean(Ordinal("Cp_IsActive")),
                            IsForNewUsersOnly = reader.GetBoolean(
                                Ordinal("Cp_IsForNewUsersOnly")),
                            CreatedAt = reader.GetDateTime(Ordinal("Cp_CreatedAt")),
                            UpdatedAt = reader.GetDateTime(Ordinal("Cp_UpdatedAt"))
                        };
                    }
                }
            }

            return new InfrastructureResponse<Cart>
            {
                IsSuccess = true,
                Message = cart == null
                    ? "No cart found"
                    : "Cart retrieved successfully",
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
            c.id AS Id,
            c.user_id AS UserId,
            c.session_id AS SessionId,
            c.status AS Status,
            c.created_at AS CreatedAt,
            c.updated_at AS UpdatedAt,
            c.version AS Version,
            c.notes AS Notes,

            ci.id AS Ci_Id,
            ci.product_id AS Ci_ProductId,
            ci.quantity AS Ci_Quantity,
            ci.unit_price AS Ci_UnitPrice,
            ci.created_at AS Ci_CreatedAt,
            ci.updated_at AS Ci_UpdatedAt,

            p.id AS P_Id,
            p.name AS P_Name,
            p.description AS P_Description,
            p.stock_quantity AS P_StockQuantity,
            p.image_url AS P_ImageUrl,
            p.price AS P_Price,
            p.reserved_stock AS P_ReservedStock,
            p.low_stock_threshold AS P_LowStockThreshold,
            p.is_active AS P_IsActive,
            p.sku AS P_SKU,
            p.created_at AS P_CreatedAt,
            p.updated_at AS P_UpdatedAt,

            cc.id AS Cc_Id,
            cc.coupon_id AS Cc_CouponId,
            cc.discount_amount AS Cc_DiscountAmount,
            cc.created_at AS Cc_CreatedAt,
            cc.updated_at AS Cc_UpdatedAt,
            cc.applied_at AS Cc_AppliedAt,

            cpn.id AS Cp_Id,
            cpn.code AS Cp_Code,
            cpn.description AS Cp_Description,
            cpn.discount_amount AS Cp_DiscountAmount,
            cpn.minimum_order_amount AS Cp_MinimumOrderAmount,
            cpn.type AS Cp_Type,
            cpn.value AS Cp_Value,
            cpn.is_active AS Cp_IsActive,
            cpn.is_for_new_users_only AS Cp_IsForNewUsersOnly,
            cpn.created_at AS Cp_CreatedAt,
            cpn.updated_at AS Cp_UpdatedAt

        FROM shopping_carts c
        LEFT JOIN cart_items ci
            ON c.id = ci.cart_id
        LEFT JOIN products p
            ON ci.product_id = p.id
        LEFT JOIN cart_coupons cc
            ON c.id = cc.cart_id
        LEFT JOIN coupons cpn
            ON cc.coupon_id = cpn.id

        WHERE c.session_id = (
            SELECT c2.session_id
            FROM shopping_carts c2
            WHERE c2.session_id = @SessionId
              AND c2.status = @Status
            ORDER BY c2.id
            LIMIT 1
        )

        ORDER BY ci.id, cc.id";

            using var connection = await OpenConnectionAsync();

            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@SessionId", sessionId);
            command.Parameters.AddWithValue(
                "@Status",
                (status ?? CartStatus.Active).ToString());

            await using var reader = await command.ExecuteReaderAsync();

            Cart? cart = null;

            var cartItems = new Dictionary<int, CartItem>();
            var cartCoupons = new Dictionary<int, CartCoupon>();

            int Ordinal(string name) => reader.GetOrdinal(name);

            string? NullableString(string name) =>
                reader.IsDBNull(Ordinal(name))
                    ? null
                    : reader.GetString(Ordinal(name));

            while (await reader.ReadAsync())
            {
                int cartId = reader.GetInt32(Ordinal("Id"));

                if (cart == null)
                {
                    cart = new Cart
                    {
                        Id = cartId,
                        UserId = reader.IsDBNull(Ordinal("UserId"))
                            ? null
                            : reader.GetInt32(Ordinal("UserId")),
                        SessionId = NullableString("SessionId"),
                        Status = Enum.Parse<CartStatus>(
                            reader.GetString(Ordinal("Status"))),
                        CreatedAt = reader.GetDateTime(Ordinal("CreatedAt")),
                        UpdatedAt = reader.GetDateTime(Ordinal("UpdatedAt")),
                        Version = reader.GetInt32(Ordinal("Version")),
                        Note = NullableString("Notes"),
                        CartItems = new List<CartItem>(),
                        CartCoupons = new List<CartCoupon>()
                    };
                }

                // Map cart items.
                if (!reader.IsDBNull(Ordinal("Ci_Id")))
                {
                    int cartItemId = reader.GetInt32(Ordinal("Ci_Id"));

                    if (!cartItems.TryGetValue(cartItemId, out var cartItem))
                    {
                        cartItem = new CartItem
                        {
                            Id = cartItemId,
                            ShoppingCartId = cartId,
                            ProductId = reader.GetInt32(Ordinal("Ci_ProductId")),
                            Quantity = reader.GetInt32(Ordinal("Ci_Quantity")),
                            UnitPrice = reader.GetDecimal(Ordinal("Ci_UnitPrice")),
                            CreatedAt = reader.GetDateTime(Ordinal("Ci_CreatedAt")),
                            UpdatedAt = reader.GetDateTime(Ordinal("Ci_UpdatedAt"))
                        };

                        cartItems.Add(cartItemId, cartItem);
                        cart.CartItems.Add(cartItem);
                    }

                    // Map the associated product once per cart item.
                    if (cartItem.Product == null &&
                        !reader.IsDBNull(Ordinal("P_Id")))
                    {
                        cartItem.Product = new Product
                        {
                            Id = reader.GetInt32(Ordinal("P_Id")),
                            Name = reader.GetString(Ordinal("P_Name")),
                            Description = NullableString("P_Description"),
                            StockQuantity = reader.GetInt32(Ordinal("P_StockQuantity")),
                            ImageUrl = NullableString("P_ImageUrl"),
                            Price = reader.GetDecimal(Ordinal("P_Price")),
                            ReservedStock = reader.GetInt32(Ordinal("P_ReservedStock")),
                            LowStockThreshold = reader.GetInt32(Ordinal("P_LowStockThreshold")),
                            IsActive = reader.GetBoolean(Ordinal("P_IsActive")),
                            SKU = NullableString("P_SKU"),
                            CreatedAt = reader.GetDateTime(Ordinal("P_CreatedAt")),
                            UpdatedAt = reader.GetDateTime(Ordinal("P_UpdatedAt"))
                        };
                    }
                }

                // Map applied coupons.
                if (!reader.IsDBNull(Ordinal("Cc_Id")))
                {
                    int cartCouponId = reader.GetInt32(Ordinal("Cc_Id"));

                    if (!cartCoupons.TryGetValue(cartCouponId, out var cartCoupon))
                    {
                        cartCoupon = new CartCoupon
                        {
                            Id = cartCouponId,
                            CartId = cartId,
                            CouponId = reader.GetInt32(Ordinal("Cc_CouponId")),
                            DiscountAmount = reader.GetDecimal(Ordinal("Cc_DiscountAmount")),
                            CreatedAt = reader.GetDateTime(Ordinal("Cc_CreatedAt")),
                            UpdatedAt = reader.GetDateTime(Ordinal("Cc_UpdatedAt")),
                            AppliedAt = reader.GetDateTime(Ordinal("Cc_AppliedAt"))
                        };

                        cartCoupons.Add(cartCouponId, cartCoupon);
                        cart.CartCoupons.Add(cartCoupon);
                    }

                    if (cartCoupon.Coupon == null &&
                        !reader.IsDBNull(Ordinal("Cp_Id")))
                    {
                        cartCoupon.Coupon = new Coupon
                        {
                            Id = reader.GetInt32(Ordinal("Cp_Id")),
                            Code = reader.GetString(Ordinal("Cp_Code")),
                            Description = NullableString("Cp_Description"),
                            DiscountAmount = reader.GetDecimal(Ordinal("Cp_DiscountAmount")),
                            MinimumOrderAmount = reader.GetDecimal(Ordinal("Cp_MinimumOrderAmount")),
                            Type = (CouponType)
                                reader.GetInt16(Ordinal("Cp_Type")),
                            Value = reader.GetDecimal(Ordinal("Cp_Value")),
                            IsActive = reader.GetBoolean(Ordinal("Cp_IsActive")),
                            IsForNewUsersOnly = reader.GetBoolean(
                                Ordinal("Cp_IsForNewUsersOnly")),
                            CreatedAt = reader.GetDateTime(Ordinal("Cp_CreatedAt")),
                            UpdatedAt = reader.GetDateTime(Ordinal("Cp_UpdatedAt"))
                        };
                    }
                }
            }

            return new InfrastructureResponse<Cart>
            {
                IsSuccess = true,
                Message = cart == null
                    ? "No cart found"
                    : "Cart retrieved successfully",
                Value = cart
            };
        }

        public async Task<InfrastructureResponse<List<Cart>>> GetCartsAsync()
        {
            const string sql = @"
                SELECT
                    c.id, c.user_id, c.session_id, c.status, c.created_at, c.updated_at, c.version, c.notes
                FROM shopping_carts c
                ORDER BY c.id";

            using var connection = await OpenConnectionAsync();

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
                SET user_id = @UserId,
                    session_id = @SessionId,
                    status = @Status,
                    updated_at = @UpdatedAt,
                    version = version + 1
                WHERE id = @Id
                RETURNING id, user_id, session_id, status, created_at, updated_at, version";

            using var connection = await OpenConnectionAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", (object?)cart.UserId ?? DBNull.Value);
            command.Parameters.AddWithValue("@SessionId", (object?)cart.SessionId ?? DBNull.Value);
            command.Parameters.AddWithValue("@Status", cart.Status.ToString());
            command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("@Id", cart.Id);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                cart.Id = reader.GetInt32(reader.GetOrdinal("id"));
                cart.UserId = reader.IsDBNull(reader.GetOrdinal("user_id")) ? null : reader.GetInt32(reader.GetOrdinal("user_id"));
                cart.SessionId = reader.IsDBNull(reader.GetOrdinal("session_id")) ? null : reader.GetString(reader.GetOrdinal("session_id"));
                cart.Status = Enum.Parse<CartStatus>(reader.GetString(reader.GetOrdinal("status")));
                cart.CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"));
                cart.UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"));
                cart.Version = reader.GetInt32(reader.GetOrdinal("version"));
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

            using var connection = await OpenConnectionAsync();

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
                RETURNING id, user_id, session_id, status, created_at, updated_at, version, notes";

            using var connection = await OpenConnectionAsync();

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

        public async Task<InfrastructureResponse<Cart>> GetCartByIdAsync(int id, CartStatus? status = CartStatus.Active)
        {
            if (id <= 0)
                return new InfrastructureResponse<Cart>()
                {
                    IsSuccess = false,
                    Message = "Invalid cart ID",
                    Value = null
                };

            const string sql = @"
        SELECT
            c.id AS Id,
            c.user_id AS UserId,
            c.session_id AS SessionId,
            c.status AS Status,
            c.created_at AS CreatedAt,
            c.updated_at AS UpdatedAt,
            c.version AS Version,
            c.notes AS Notes,

            ci.id AS Ci_Id,
            ci.product_id AS Ci_ProductId,
            ci.quantity AS Ci_Quantity,
            ci.unit_price AS Ci_UnitPrice,
            ci.created_at AS Ci_CreatedAt,
            ci.updated_at AS Ci_UpdatedAt,

            p.id AS P_Id,
            p.name AS P_Name,
            p.description AS P_Description,
            p.stock_quantity AS P_StockQuantity,
            p.image_url AS P_ImageUrl,
            p.price AS P_Price,
            p.reserved_stock AS P_ReservedStock,
            p.low_stock_threshold AS P_LowStockThreshold,
            p.is_active AS P_IsActive,
            p.sku AS P_SKU,
            p.created_at AS P_CreatedAt,
            p.updated_at AS P_UpdatedAt,

            cc.id AS Cc_Id,
            cc.coupon_id AS Cc_CouponId,
            cc.discount_amount AS Cc_DiscountAmount,
            cc.created_at AS Cc_CreatedAt,
            cc.updated_at AS Cc_UpdatedAt,
            cc.applied_at AS Cc_AppliedAt,

            cpn.id AS Cp_Id,
            cpn.code AS Cp_Code,
            cpn.description AS Cp_Description,
            cpn.discount_amount AS Cp_DiscountAmount,
            cpn.minimum_order_amount AS Cp_MinimumOrderAmount,
            cpn.type AS Cp_Type,
            cpn.value AS Cp_Value,
            cpn.is_active AS Cp_IsActive,
            cpn.is_for_new_users_only AS Cp_IsForNewUsersOnly,
            cpn.created_at AS Cp_CreatedAt,
            cpn.updated_at AS Cp_UpdatedAt

        FROM shopping_carts c
        LEFT JOIN cart_items ci
            ON c.id = ci.cart_id
        LEFT JOIN products p
            ON ci.product_id = p.id
        LEFT JOIN cart_coupons cc
            ON c.id = cc.cart_id
        LEFT JOIN coupons cpn
            ON cc.coupon_id = cpn.id

        WHERE c.id = (
            SELECT c2.id
            FROM shopping_carts c2
            WHERE c2.id = @CartId
              AND c2.status = @Status
            ORDER BY c2.id
            LIMIT 1
        )

        ORDER BY ci.id, cc.id";

            using var connection = await OpenConnectionAsync();

            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@CartId", id);
            command.Parameters.AddWithValue(
                "@Status",
                (status ?? CartStatus.Active).ToString());

            await using var reader = await command.ExecuteReaderAsync();

            Cart? cart = null;

            var cartItems = new Dictionary<int, CartItem>();
            var cartCoupons = new Dictionary<int, CartCoupon>();

            int Ordinal(string name) => reader.GetOrdinal(name);

            string? NullableString(string name) =>
                reader.IsDBNull(Ordinal(name))
                    ? null
                    : reader.GetString(Ordinal(name));

            while (await reader.ReadAsync())
            {
                int cartId = reader.GetInt32(Ordinal("Id"));

                if (cart == null)
                {
                    cart = new Cart
                    {
                        Id = cartId,
                        UserId = reader.IsDBNull(Ordinal("UserId"))
                            ? null
                            : reader.GetInt32(Ordinal("UserId")),
                        SessionId = NullableString("SessionId"),
                        Status = Enum.Parse<CartStatus>(
                            reader.GetString(Ordinal("Status"))),
                        CreatedAt = reader.GetDateTime(Ordinal("CreatedAt")),
                        UpdatedAt = reader.GetDateTime(Ordinal("UpdatedAt")),
                        Version = reader.GetInt32(Ordinal("Version")),
                        Note = NullableString("Notes"),
                        CartItems = new List<CartItem>(),
                        CartCoupons = new List<CartCoupon>()
                    };
                }

                // Map cart items.
                if (!reader.IsDBNull(Ordinal("Ci_Id")))
                {
                    int cartItemId = reader.GetInt32(Ordinal("Ci_Id"));

                    if (!cartItems.TryGetValue(cartItemId, out var cartItem))
                    {
                        cartItem = new CartItem
                        {
                            Id = cartItemId,
                            ShoppingCartId = cartId,
                            ProductId = reader.GetInt32(Ordinal("Ci_ProductId")),
                            Quantity = reader.GetInt32(Ordinal("Ci_Quantity")),
                            UnitPrice = reader.GetDecimal(Ordinal("Ci_UnitPrice")),
                            CreatedAt = reader.GetDateTime(Ordinal("Ci_CreatedAt")),
                            UpdatedAt = reader.GetDateTime(Ordinal("Ci_UpdatedAt"))
                        };

                        cartItems.Add(cartItemId, cartItem);
                        cart.CartItems.Add(cartItem);
                    }

                    // Map the associated product once per cart item.
                    if (cartItem.Product == null &&
                        !reader.IsDBNull(Ordinal("P_Id")))
                    {
                        cartItem.Product = new Product
                        {
                            Id = reader.GetInt32(Ordinal("P_Id")),
                            Name = reader.GetString(Ordinal("P_Name")),
                            Description = NullableString("P_Description"),
                            StockQuantity = reader.GetInt32(Ordinal("P_StockQuantity")),
                            ImageUrl = NullableString("P_ImageUrl"),
                            Price = reader.GetDecimal(Ordinal("P_Price")),
                            ReservedStock = reader.GetInt32(Ordinal("P_ReservedStock")),
                            LowStockThreshold = reader.GetInt32(Ordinal("P_LowStockThreshold")),
                            IsActive = reader.GetBoolean(Ordinal("P_IsActive")),
                            SKU = NullableString("P_SKU"),
                            CreatedAt = reader.GetDateTime(Ordinal("P_CreatedAt")),
                            UpdatedAt = reader.GetDateTime(Ordinal("P_UpdatedAt"))
                        };
                    }
                }

                // Map applied coupons.
                if (!reader.IsDBNull(Ordinal("Cc_Id")))
                {
                    int cartCouponId = reader.GetInt32(Ordinal("Cc_Id"));

                    if (!cartCoupons.TryGetValue(cartCouponId, out var cartCoupon))
                    {
                        cartCoupon = new CartCoupon
                        {
                            Id = cartCouponId,
                            CartId = cartId,
                            CouponId = reader.GetInt32(Ordinal("Cc_CouponId")),
                            DiscountAmount = reader.GetDecimal(Ordinal("Cc_DiscountAmount")),
                            CreatedAt = reader.GetDateTime(Ordinal("Cc_CreatedAt")),
                            UpdatedAt = reader.GetDateTime(Ordinal("Cc_UpdatedAt")),
                            AppliedAt = reader.GetDateTime(Ordinal("Cc_AppliedAt"))
                        };

                        cartCoupons.Add(cartCouponId, cartCoupon);
                        cart.CartCoupons.Add(cartCoupon);
                    }

                    if (cartCoupon.Coupon == null &&
                        !reader.IsDBNull(Ordinal("Cp_Id")))
                    {
                        cartCoupon.Coupon = new Coupon
                        {
                            Id = reader.GetInt32(Ordinal("Cp_Id")),
                            Code = reader.GetString(Ordinal("Cp_Code")),
                            Description = NullableString("Cp_Description"),
                            DiscountAmount = reader.GetDecimal(Ordinal("Cp_DiscountAmount")),
                            MinimumOrderAmount = reader.GetDecimal(Ordinal("Cp_MinimumOrderAmount")),
                            Type = 
                                (CouponType)reader.GetInt16(Ordinal("Cp_Type")),
                            Value = reader.GetDecimal(Ordinal("Cp_Value")),
                            IsActive = reader.GetBoolean(Ordinal("Cp_IsActive")),
                            IsForNewUsersOnly = reader.GetBoolean(
                                Ordinal("Cp_IsForNewUsersOnly")),
                            CreatedAt = reader.GetDateTime(Ordinal("Cp_CreatedAt")),
                            UpdatedAt = reader.GetDateTime(Ordinal("Cp_UpdatedAt"))
                        };
                    }
                }
            }

            return new InfrastructureResponse<Cart>
            {
                IsSuccess = true,
                Message = cart == null
                    ? "No cart found"
                    : "Cart retrieved successfully",
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

            using var connection = await OpenConnectionAsync();

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

            using var connection = await OpenConnectionAsync();

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

            using var connection = await OpenConnectionAsync();

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

                using var connection = await OpenConnectionAsync();

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

            using var connection = await OpenConnectionAsync();

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
            
            using var connection = await OpenConnectionAsync(); 

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

            using var connection = await OpenConnectionAsync();

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
            int notesOrdinal = -1;
            try { notesOrdinal = reader.GetOrdinal("notes"); } catch { }

            return new Cart()
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                UserId = reader.IsDBNull(reader.GetOrdinal("user_id")) ? null : reader.GetInt32(reader.GetOrdinal("user_id")),
                SessionId = reader.IsDBNull(reader.GetOrdinal("session_id")) ? null : reader.GetString(reader.GetOrdinal("session_id")),
                Status = Enum.Parse<CartStatus>(reader.GetString(reader.GetOrdinal("status"))),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at")),
                Version = reader.GetInt32(reader.GetOrdinal("version")),
                Note = notesOrdinal >= 0 && !reader.IsDBNull(notesOrdinal) ? reader.GetString(notesOrdinal) : null
            };
        }

        private async Task<NpgsqlConnection> OpenConnectionAsync()
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            return connection;
        }

        public async Task<bool> ClearItemsRelatedToCartId(int cartId)
        {
            string query = @"delete from cart_items where cart_id = @CartId;";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@CartId", cartId);

            var rowEffected = await command.ExecuteScalarAsync();

            return (int)rowEffected != 0;
        }

        public async Task<bool> ClearCouponsRelatedToCouponId(int cartId)
        {
            string query = @"delete from cart_coupons where cart_id = @CartId;";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@CartId", cartId);

            var rowEffected = await command.ExecuteScalarAsync();

            return (int)rowEffected != 0;
        }

        public Task<bool> ClearCouponsRelatedToCartId(int cartId)
        {
            throw new NotImplementedException();
        }

        public async Task<InfrastructureResponse<List<CartItem>>> GetItemsRelatedToCartId(int cartId)
        {
            string query = @"select * from cart_items where cart_id = @CartId;";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@CartId", cartId);

            List<CartItem> items = new List<CartItem>();

            var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                items.Add(MapCartItem(reader));
            }

            return new InfrastructureResponse<List<CartItem>>()
            {
                IsSuccess = true,
                Value = items
            };
        }

        private CartItem MapCartItem(NpgsqlDataReader reader)
        {
            return new CartItem()
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                ShoppingCartId = reader.GetInt32(reader.GetOrdinal("cart_id")),
                ProductId = reader.GetInt32(reader.GetOrdinal("product_id")),
                UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                Quantity = reader.GetInt32(reader.GetOrdinal("quantity")),
                UnitPrice = reader.GetDecimal(reader.GetOrdinal("unit_price")),
                SessionId = reader.IsDBNull(reader.GetOrdinal("session_id")) ? null : reader.GetString(reader.GetOrdinal("session_id")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
            };
        }

        private async Task<InfrastructureResponse<int>> AddCartCouponAsync(CartCoupon cartCoupon)
        {
            string query = @"Insert into cart_coupons (cart_id, coupon_id, user_id, discount_amount, applied_at, session_id, created_at, updated_at)
                            values (@CartId, @CouponId, @UserId, @DiscountAmount, @AppliedAt, @SessionId, @CreatedAt, @UpdatedAt)
                            returning id;";

            using var connection = await OpenConnectionAsync();

            using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@CartId", cartCoupon.CartId);
            command.Parameters.AddWithValue("@CouponId", cartCoupon.CouponId);
            command.Parameters.AddWithValue("@UserId", cartCoupon.UserId);
            command.Parameters.AddWithValue("@DiscountAmount", cartCoupon.DiscountAmount);
            command.Parameters.AddWithValue("@AppliedAt", cartCoupon.AppliedAt);
            command.Parameters.AddWithValue("@SessionId", (object?)cartCoupon.SessionId ?? DBNull.Value);
            command.Parameters.AddWithValue("@CreatedAt", cartCoupon.CreatedAt);
            command.Parameters.AddWithValue("@UpdatedAt", cartCoupon.UpdatedAt);

            var rowEffected = await command.ExecuteScalarAsync();

            return new InfrastructureResponse<int>()
            {
                IsSuccess = true,
                Value = Convert.ToInt16(rowEffected)
            };
        }

        public async Task<InfrastructureResponse<bool>> AddCartCouponsAsync(Cart cart)
        {
            foreach(var item in cart.CartCoupons)
            {
                await AddCartCouponAsync(item);
            }

            return new InfrastructureResponse<bool>()
            {
                IsSuccess = true,
                Value = true
            };
        }
    }
}