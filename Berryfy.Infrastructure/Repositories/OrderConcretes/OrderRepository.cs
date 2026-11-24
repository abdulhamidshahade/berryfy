using Berryfy.Domain.Constants;
using Berryfy.Domain.Entities;
using Berryfy.Domain.Entities.AuthEntities;
using Berryfy.Domain.Entities.OrderEntities;
using Berryfy.Domain.Entities.ProductEntities;
using Berryfy.Domain.Repositories.OrderInterfaces;
using Berryfy.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.ComponentModel;

namespace Berryfy.Infrastructure.Repositories.OrderConcretes
{
    public class OrderRepository : IOrderRepository
    {
        private readonly string _connectionString;

        public OrderRepository(IConfiguration config)
        {
            _connectionString = PostgresConnectionStrings.Resolve(config);
        }

        public async Task<InfrastructureResponse<Order?>> GetOrderByIdAsync(int orderId)
        {
            const string query = @"
                SELECT id, user_id, cart_id, session_id, is_paid, status, subtotal,
                       tax_amount, shipping_amount, total, discount_total, customer_email, customer_phone,
                       reference_number, completed_at, cancelled_at, shipping_name, shipping_address1, shipping_address2,
                       shipping_city, shipping_state, shipping_postal_code, shipping_country,
                       created_at, updated_at
                FROM orders o
                WHERE o.id = @OrderId;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@OrderId", orderId);

            Order? order = null;
            await using (var reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    order = MapOrder(reader);
                }
            }

            if (order != null)
            {
                await LoadOrderItemsAsync(connection, new List<Order> { order });
            }

            return new InfrastructureResponse<Order?>()
            {
                IsSuccess = order != null,
                Message = order != null ? "The process completed successfully" : "Order not found",
                Value = order
            };
        }

        public async Task<InfrastructureResponse<List<Order>>> GetUserOrdersAsync(int userId, int page = 1, int pageSize = 10)
        {
            const string query = @"
                SELECT id, user_id, cart_id, session_id, is_paid, status, subtotal, tax_amount, shipping_amount, total,
                       discount_total, customer_email, customer_phone, reference_number, completed_at, cancelled_at,
                       shipping_name, shipping_address1, shipping_address2, shipping_city, shipping_state,
                       shipping_postal_code, shipping_country, created_at, updated_at
                FROM orders o
                WHERE o.user_id = @UserId
                ORDER BY created_at DESC
                OFFSET @Offset
                LIMIT @PageSize;";

            await using var connection = await OpenConnectionAsync();
            var offset = Math.Max(0, (page - 1) * pageSize);

            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@Offset", offset);
            command.Parameters.AddWithValue("@PageSize", pageSize);

            List<Order> orders = new List<Order>();

            await using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    orders.Add(MapOrder(reader));
                }
            }

            if (orders.Count > 0)
            {
                await LoadOrderItemsAsync(connection, orders);
            }

            return new InfrastructureResponse<List<Order>>()
            {
                IsSuccess = true,
                Message = "The process completed successfully",
                Value = orders
            };
        }

        public async Task<InfrastructureResponse<List<Order>>> GetAllOrdersAsync(int page = 1, int pageSize = 50)
        {
            const string query = @"
                SELECT
                    o.id,
                    o.user_id,
                    o.cart_id,
                    o.status,
                    o.subtotal,
                    o.tax_amount,
                    o.shipping_amount,
                    o.total,
                    o.discount_total,
                    o.customer_email,
                    o.customer_phone,
                    o.reference_number,
                    o.completed_at,
                    o.cancelled_at,
                    o.shipping_name,
                    o.shipping_address1,
                    o.shipping_address2,
                    o.shipping_city,
                    o.shipping_state,
                    o.shipping_postal_code,
                    o.shipping_country,
                    o.created_at,
                    o.updated_at,
                    o.is_paid,
                    o.session_id
                FROM orders o
                ORDER BY o.created_at DESC
                OFFSET @Offset
                LIMIT @PageSize;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(query, connection);

            int offset = Math.Max(0, (page - 1) * pageSize);
            command.Parameters.AddWithValue("@Offset", offset);
            command.Parameters.AddWithValue("@PageSize", pageSize);

            List<Order> orders = new List<Order>();
            await using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    orders.Add(MapOrder(reader));
                }
            }

            if (orders.Count > 0)
            {
                await LoadOrderItemsAsync(connection, orders);
            }

            return new InfrastructureResponse<List<Order>>()
            {
                IsSuccess = true,
                Message = "The process completed successfully",
                Value = orders
            };
        }

        public async Task<InfrastructureResponse<bool>> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus)
        {
            const string query = @"
                UPDATE orders
                SET status = @newStatus,
                    updated_at = CURRENT_TIMESTAMP,
                    completed_at = CASE WHEN @newStatus = @completedAtStatus THEN CURRENT_TIMESTAMP ELSE completed_at END,
                    cancelled_at = CASE WHEN @newStatus = @cancelledAtStatus THEN CURRENT_TIMESTAMP ELSE cancelled_at END
                WHERE id = @orderId;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@newStatus", (int)newStatus);
            command.Parameters.AddWithValue("@completedAtStatus", (int)OrderStatus.Completed);
            command.Parameters.AddWithValue("@cancelledAtStatus", (int)OrderStatus.Cancelled);
            command.Parameters.AddWithValue("@orderId", orderId);

            var rowEffected = await command.ExecuteNonQueryAsync();

            return new InfrastructureResponse<bool>()
            {
                IsSuccess = rowEffected > 0,
                Message = rowEffected > 0 ? "The process completed successfully" : "No rows were affected",
                Value = rowEffected > 0
            };
        }

        public Task<string> GenerateUniqueReferenceNumberAsync()
        {
            var uniqueRef = $"ORD-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
            return Task.FromResult(uniqueRef);
        }

        public async Task<InfrastructureResponse<Order>> CreateOrderAsync(Order order)
        {
            const string query = @"
                INSERT INTO orders (user_id, cart_id, status, subtotal, tax_amount, shipping_amount,
                    total, discount_total, customer_email, customer_phone, reference_number, completed_at, cancelled_at, shipping_name,
                    shipping_address1, shipping_address2, shipping_city, shipping_state, shipping_postal_code, shipping_country,
                    created_at, updated_at, is_paid, session_id)
                VALUES (@UserId, @CartId, @Status, @SubTotal, @TaxAmount, @ShippingAmount,
                    @Total, @DiscountTotal, @CustomerEmail, @CustomerPhone, @ReferenceNumber, @CompletedAt, @CancelledAt, @ShippingName,
                    @ShippingAddress1, @ShippingAddress2, @ShippingCity, @ShippingState, @ShippingPostalCode, @ShippingCountry,
                    @CreatedAt, @UpdatedAt, @IsPaid, @SessionId)
                RETURNING *;";

            var now = DateTime.UtcNow;
            if (order.CreatedAt == default) order.CreatedAt = now;
            if (order.UpdatedAt == default) order.UpdatedAt = now;

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(query, connection);

            AddOrderParameters(command, order);

            await using var reader = await command.ExecuteReaderAsync();
            Order? orderObj = null;

            if (await reader.ReadAsync())
            {
                orderObj = MapOrder(reader);
            }

            return new InfrastructureResponse<Order>()
            {
                IsSuccess = orderObj != null,
                Message = orderObj != null ? "The process completed successfully" : "Failed to create order",
                Value = orderObj!
            };
        }

        public async Task<InfrastructureResponse<OrderItem>> CreateOrderItemAsync(OrderItem item)
        {
            const string query = @"
                INSERT INTO order_items (order_id, product_id, quantity, unit_price, total_price,
                    discount_amount, created_at, updated_at, product_name)
                VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice,
                    @TotalPrice, @DiscountAmount, @CreatedAt, @UpdatedAt, @ProductName)
                RETURNING *;";

            var now = DateTime.UtcNow;
            if (item.CreatedAt == default) item.CreatedAt = now;
            if (item.UpdatedAt == default) item.UpdatedAt = now;

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(query, connection);

            AddOrderItemParameters(command, item);

            await using var reader = await command.ExecuteReaderAsync();
            OrderItem? orderItem = null;

            if (await reader.ReadAsync())
            {
                orderItem = MapOrderItem(reader);
            }

            return new InfrastructureResponse<OrderItem>()
            {
                IsSuccess = orderItem != null,
                Message = orderItem != null ? "The process completed successfully" : "Failed to create order item",
                Value = orderItem!
            };
        }

        public async Task<InfrastructureResponse<Order?>> GetOrderByReferenceNumberAsync(string referenceNumber)
        {
            const string query = @"
                SELECT o.*
                FROM orders o
                WHERE o.reference_number = @ReferenceNumber;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@ReferenceNumber", referenceNumber);

            Order? order = null;
            await using (var reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    order = MapOrder(reader);
                }
            }

            if (order != null)
            {
                await LoadOrderItemsAsync(connection, new List<Order> { order });
            }

            return new InfrastructureResponse<Order?>()
            {
                IsSuccess = order != null,
                Message = order != null ? "The process completed successfully" : "Order not found",
                Value = order
            };
        }

        public async Task<InfrastructureResponse<List<Order>>> GetOrdersByStatusAsync(OrderStatus status, int page = 1, int pageSize = 10)
        {
            const string query = @"
                SELECT * FROM orders
                WHERE status = @OrderStatus
                ORDER BY created_at DESC
                OFFSET @offset LIMIT @pageSize;";

            int offset = Math.Max(0, (page - 1) * pageSize);

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@offset", offset);
            command.Parameters.AddWithValue("@pageSize", pageSize);
            command.Parameters.AddWithValue("@OrderStatus", (int)status);

            List<Order> orders = new List<Order>();

            await using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    orders.Add(MapOrder(reader));
                }
            }

            if (orders.Count > 0)
            {
                await LoadOrderItemsAsync(connection, orders);
            }

            return new InfrastructureResponse<List<Order>>()
            {
                IsSuccess = true,
                Message = "The process completed successfully",
                Value = orders
            };
        }

        public async Task<InfrastructureResponse<bool>> UpdateOrderPaymentStatusAsync(int orderId, PaymentStatus paymentStatus)
        {
            const string query = @"
                UPDATE orders
                SET is_paid = (@paymentStatus = @completedStatus),
                    updated_at = CURRENT_TIMESTAMP
                WHERE id = @orderId;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@paymentStatus", (int)paymentStatus);
            command.Parameters.AddWithValue("@completedStatus", (int)PaymentStatus.Completed);
            command.Parameters.AddWithValue("@orderId", orderId);

            int rowEffected = await command.ExecuteNonQueryAsync();

            return new InfrastructureResponse<bool>()
            {
                IsSuccess = rowEffected > 0,
                Message = rowEffected > 0 ? "The process completed successfully" : "No rows affected",
                Value = rowEffected > 0
            };
        }

        public async Task<InfrastructureResponse<Order?>> GetOrderByCartIdAsync(int cartId)
        {
            const string query = @"
                SELECT *
                FROM orders
                WHERE cart_id = @cartId
                ORDER BY id DESC
                LIMIT 1;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@cartId", cartId);

            Order? order = null;
            await using (var reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    order = MapOrder(reader);
                }
            }

            if (order != null)
            {
                await LoadOrderItemsAsync(connection, new List<Order> { order });
            }

            return new InfrastructureResponse<Order?>()
            {
                IsSuccess = order != null,
                Message = order != null ? "The process completed successfully" : "Order not found",
                Value = order
            };
        }

        public async Task<InfrastructureResponse<bool>> UpdateOrderAsync(Order order)
        {
            const string query = @"
                UPDATE orders
                SET user_id = @UserId,
                    cart_id = @CartId,
                    status = @Status,
                    subtotal = @SubTotal,
                    tax_amount = @TaxAmount,
                    shipping_amount = @ShippingAmount,
                    total = @Total,
                    discount_total = @DiscountTotal,
                    customer_email = @CustomerEmail,
                    customer_phone = @CustomerPhone,
                    reference_number = @ReferenceNumber,
                    completed_at = @CompletedAt,
                    cancelled_at = @CancelledAt,
                    shipping_name = @ShippingName,
                    shipping_address1 = @ShippingAddress1,
                    shipping_address2 = @ShippingAddress2,
                    shipping_city = @ShippingCity,
                    shipping_state = @ShippingState,
                    shipping_postal_code = @ShippingPostalCode,
                    shipping_country = @ShippingCountry,
                    created_at = @CreatedAt,
                    updated_at = @UpdatedAt,
                    is_paid = @IsPaid,
                    session_id = @SessionId
                WHERE id = @orderId;";

            order.UpdatedAt = DateTime.UtcNow;

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(query, connection);

            AddOrderParameters(command, order);
            command.Parameters.AddWithValue("@orderId", order.Id);

            int rowEffected = await command.ExecuteNonQueryAsync();

            return new InfrastructureResponse<bool>()
            {
                IsSuccess = rowEffected > 0,
                Message = "The process completed successfully",
                Value = rowEffected > 0
            };
        }

        public async Task<InfrastructureResponse<bool>> DeleteOrderItemsAsync(int orderId)
        {
            const string query = @"DELETE FROM order_items oi WHERE oi.order_id = @orderId;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@orderId", orderId);

            int rowEffected = await command.ExecuteNonQueryAsync();

            return new InfrastructureResponse<bool>()
            {
                IsSuccess = true,
                Message = "The process completed successfully",
                Value = rowEffected >= 0
            };
        }

        public async Task<InfrastructureResponse<bool>> UserHasPaidOrderAsync(int userId)
        {
            const string query = @"
                SELECT EXISTS (
                    SELECT 1
                    FROM orders
                    WHERE user_id = @userId AND is_paid = true
                );";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@userId", userId);

            object? result = await command.ExecuteScalarAsync();

            return new InfrastructureResponse<bool>()
            {
                IsSuccess = true,
                Message = "The process completed successfully",
                Value = result != null && Convert.ToBoolean(result)
            };
        }

        private async Task LoadOrderItemsAsync(NpgsqlConnection connection, IList<Order> orders)
        {
            if (orders.Count == 0) return;

            var orderIds = orders.Select(o => o.Id).Distinct().ToArray();
            const string sql = @"
                SELECT id, order_id, product_id, product_name, quantity, unit_price,
                       total_price, discount_amount, created_at, updated_at
                FROM order_items
                WHERE order_id = ANY(@OrderIds)
                ORDER BY id;";

            await using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@OrderIds", orderIds);

            var lookup = orders.ToDictionary(o => o.Id);
            foreach (var o in orders)
            {
                o.OrderItems ??= new List<OrderItem>();
                o.OrderItems.Clear();
            }

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var item = MapOrderItem(reader);
                if (lookup.TryGetValue(item.OrderId, out var parentOrder))
                {
                    parentOrder.OrderItems.Add(item);
                }
            }
        }

        private async Task<NpgsqlConnection> OpenConnectionAsync()
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        private static string? GetNullableString(NpgsqlDataReader reader, string column)
        {
            try
            {
                int ord = reader.GetOrdinal(column);
                return reader.IsDBNull(ord) ? null : reader.GetString(ord);
            }
            catch (IndexOutOfRangeException)
            {
                return null;
            }
        }

        private static DateTime? GetNullableDateTime(NpgsqlDataReader reader, string column)
        {
            try
            {
                int ord = reader.GetOrdinal(column);
                return reader.IsDBNull(ord) ? null : reader.GetDateTime(ord);
            }
            catch (IndexOutOfRangeException)
            {
                return null;
            }
        }

        private Order MapOrder(NpgsqlDataReader reader)
        {
            return new Order()
            {
                Id = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("id"))),
                UserId = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("user_id"))),
                CartId = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("cart_id"))),
                sessionId = GetNullableString(reader, "session_id"),
                isPaid = reader.GetBoolean(reader.GetOrdinal("is_paid")),
                Status = (OrderStatus)Convert.ToInt32(reader.GetValue(reader.GetOrdinal("status"))),
                SubTotal = reader.GetDecimal(reader.GetOrdinal("subtotal")),
                TaxAmount = reader.GetDecimal(reader.GetOrdinal("tax_amount")),
                ShippingAmount = reader.GetDecimal(reader.GetOrdinal("shipping_amount")),
                Total = reader.GetDecimal(reader.GetOrdinal("total")),
                DiscountTotal = reader.GetDecimal(reader.GetOrdinal("discount_total")),
                CustomerEmail = GetNullableString(reader, "customer_email") ?? string.Empty,
                CustomerPhone = GetNullableString(reader, "customer_phone"),
                ReferenceNumber = GetNullableString(reader, "reference_number"),
                CompletedAt = GetNullableDateTime(reader, "completed_at"),
                CancalledAt = GetNullableDateTime(reader, "cancelled_at"),
                ShippingName = GetNullableString(reader, "shipping_name"),
                ShippingAddress1 = GetNullableString(reader, "shipping_address1"),
                ShippingAddress2 = GetNullableString(reader, "shipping_address2"),
                ShippingCity = GetNullableString(reader, "shipping_city"),
                ShippingState = GetNullableString(reader, "shipping_state"),
                ShippingPostalCode = GetNullableString(reader, "shipping_postal_code"),
                ShippingCountry = GetNullableString(reader, "shipping_country"),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at")),
                OrderItems = new List<OrderItem>()
            };
        }

        private OrderItem MapOrderItem(NpgsqlDataReader reader)
        {
            var prodNameOrd = reader.GetOrdinal("product_name");
            return new OrderItem()
            {
                Id = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("id"))),
                OrderId = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("order_id"))),
                ProductId = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("product_id"))),
                ProductName = reader.IsDBNull(prodNameOrd) ? string.Empty : reader.GetString(prodNameOrd),
                Quantity = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("quantity"))),
                UnitPrice = reader.GetDecimal(reader.GetOrdinal("unit_price")),
                TotalPrice = reader.GetDecimal(reader.GetOrdinal("total_price")),
                DiscountAmount = reader.GetDecimal(reader.GetOrdinal("discount_amount")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
            };
        }

        private void AddOrderParameters(NpgsqlCommand command, Order order)
        {
            command.Parameters.AddWithValue("@UserId", order.UserId);
            command.Parameters.AddWithValue("@CartId", order.CartId);
            command.Parameters.AddWithValue("@Status", (int)order.Status);
            command.Parameters.AddWithValue("@SubTotal", order.SubTotal);
            command.Parameters.AddWithValue("@TaxAmount", order.TaxAmount);
            command.Parameters.AddWithValue("@ShippingAmount", order.ShippingAmount);
            command.Parameters.AddWithValue("@Total", order.Total);
            command.Parameters.AddWithValue("@DiscountTotal", order.DiscountTotal);
            command.Parameters.AddWithValue("@CustomerEmail", order.CustomerEmail ?? string.Empty);
            command.Parameters.AddWithValue("@CustomerPhone", (object?)order.CustomerPhone ?? DBNull.Value);
            command.Parameters.AddWithValue("@ReferenceNumber", (object?)order.ReferenceNumber ?? DBNull.Value);
            command.Parameters.AddWithValue("@CompletedAt", (object?)order.CompletedAt ?? DBNull.Value);
            command.Parameters.AddWithValue("@CancelledAt", (object?)order.CancalledAt ?? DBNull.Value);
            command.Parameters.AddWithValue("@ShippingName", (object?)order.ShippingName ?? DBNull.Value);
            command.Parameters.AddWithValue("@ShippingAddress1", (object?)order.ShippingAddress1 ?? DBNull.Value);
            command.Parameters.AddWithValue("@ShippingAddress2", (object?)order.ShippingAddress2 ?? DBNull.Value);
            command.Parameters.AddWithValue("@ShippingCity", (object?)order.ShippingCity ?? DBNull.Value);
            command.Parameters.AddWithValue("@ShippingState", (object?)order.ShippingState ?? DBNull.Value);
            command.Parameters.AddWithValue("@ShippingPostalCode", (object?)order.ShippingPostalCode ?? DBNull.Value);
            command.Parameters.AddWithValue("@ShippingCountry", (object?)order.ShippingCountry ?? DBNull.Value);
            command.Parameters.AddWithValue("@CreatedAt", order.CreatedAt == default ? DateTime.UtcNow : order.CreatedAt);
            command.Parameters.AddWithValue("@UpdatedAt", order.UpdatedAt == default ? DateTime.UtcNow : order.UpdatedAt);
            command.Parameters.AddWithValue("@IsPaid", order.isPaid);
            command.Parameters.AddWithValue("@SessionId", (object?)order.sessionId ?? DBNull.Value);
        }

        private void AddOrderItemParameters(NpgsqlCommand command, OrderItem orderItem)
        {
            command.Parameters.AddWithValue("@OrderId", orderItem.OrderId);
            command.Parameters.AddWithValue("@ProductId", orderItem.ProductId);
            command.Parameters.AddWithValue("@Quantity", orderItem.Quantity);
            command.Parameters.AddWithValue("@UnitPrice", orderItem.UnitPrice);
            command.Parameters.AddWithValue("@TotalPrice", orderItem.TotalPrice);
            command.Parameters.AddWithValue("@DiscountAmount", orderItem.DiscountAmount);
            command.Parameters.AddWithValue("@CreatedAt", orderItem.CreatedAt == default ? DateTime.UtcNow : orderItem.CreatedAt);
            command.Parameters.AddWithValue("@UpdatedAt", orderItem.UpdatedAt == default ? DateTime.UtcNow : orderItem.UpdatedAt);
            command.Parameters.AddWithValue("@ProductName", orderItem.ProductName ?? string.Empty);
        }
    }
}