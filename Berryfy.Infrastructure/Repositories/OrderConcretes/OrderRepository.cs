using Berryfy.Domain.Constants;
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

        public async Task<Order?> GetOrderByIdAsync(int orderId)
        {
            string query = @"select id, user_id, cart_id, is_paid, status, subtotal,
            tax_amount, shipping_amount, total, discount_total, customer_email, customer_phone,
            completed_at, cancelled_at, shipping_name, shipping_address1, shipping_address2,
            shipping_city, shipping_state, shipping_postal_code, shipping_country,
            created_at, updated_at
            from orders o
            where o.id = @OrderId;";

            avar connection = wait OpenConnectionAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@OrderId", orderId);

            Order order = new Order();

            var reader = await command.ExecuteReaderAsync();

            if(await reader.ReadAsync())
            {
                order = MapOrder(reader);
            }                

            return order;
        }

        public async Task<List<Order>> GetUserOrdersAsync(int userId, int page = 1, int pageSize = 10)
        {
            string query = @"select id, user_id, cart_id, is_paid, status, subtotal, tax_amount, shipping_amount, total
            discount_total, customer_email, customer_phone, completed_at, cancelled_at, shipping_name, shipping_address1,
            shipping_address2, shipping_city, shipping_state, shipping_postal_code, shipping_country, created_at, updated_at
            from orders o
                            where o.user_id = @UserId
                            order by created_at desc
                            offset @Offset
                            limit @PageSize;";

            var connection = await OpenConnectionAsync();

            var offset = (page - 1) * pageSize;

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@Offset", offset);
            command.Parameters.AddWithValue("@PageSize", pageSize);

            List<Order> orders = new List<Order>();

            var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                orders.Add(MapOrder(reader));
            }

            return ordersh;
        }

        public async Task<List<Order>> GetAllOrdersAsync(int page = 1, int pageSize = 50)
        {
            string query = @"SELECT
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
    o.session_id,

FROM (
    SELECT *
    FROM Orders
    ORDER BY CreatedAt DESC
    OFFSET @Offset
    LIMIT @PageSize
) o
ORDER BY o.CreatedAt DESC;";


            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(query, connection);

            int offset = (page - 1) * pageSize;

            command.Parameters.AddWithValue("@Offset", offset);
            command.Parameters.AddWithValue("@PageSize", pageSize);


            var reader = await command.ExecuteReaderAsync();

            List<Order> orders = new List<Order>();

            while(await reader.ReadAsync())
            {
                orders.Add(MapOrder(reader));
            }

            return orders();
        }


        public async Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus)
        {
            string query = @"Update Orders
                            Set Status = @newStatus,
                            UpdatedAt = 
                            CompletedAt = Case When @newStatus = @completedAtStatus Then CURRENT_TIMESTAMP else completed_at End,
                            CancelledAt = Case When @newStatus = @cancelledAtStatus Then CURRENT_TIMESTAMP else cancelled_at End
                            Where Id = @orderId;";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@newStatus", newStatus);
            command.Parameters.AddWithValue("@completedAtStatus", (int)OrderStatus.Completed);
            command.Parameters.AddWithValue("@cancelledAtStatus", (int)OrderStatus.Cancelled);

            var rowEffected = await command.ExecuteNonQueryAsync();

            return rowEffected > 0;
        }

        public Task<string> GenerateUniqueReferenceNumberAsync()
        {
            var uniqueRef = $"ORD-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
            return Task.FromResult(uniqueRef);
        }

        public async Task<Order> CreateOrderAsync(Order order)
        {
            string query = @"Insert into Orders (user_id, cart_id, status, subtotal, tax_amount, shipping_amount,
total, discount_total, customer_email, customer_phone, reference_number, completed_at, cancelled_at, shipping_name,
shipping_address1, shipping_address2, shipping_city, shipping_state, shipping_postal_code, shipping_country,
created_at, updated_at, is_paid, session_id) Values (@UserId, @CartId, @Status, @SubTotal, @TaxAmount, @ShippingAmount,
@Total, @DiscountTotal, @CustomerEmail, @CustomerPhone, @ReferenceNumber, @CompletedAt, @CancelledAt, @ShippingName,
@ShippingAddress1, @ShippingAddress2, @ShippingCity, @ShippingState, @ShippingPostalCode, @ShippingCountry, 
@CreatedAt, @UpdatedAt, @isPaid, @sessionId) Returning *;";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(query, connection);

            AddOrderParameters(command, order);

            var reader = await command.ExecuteReaderAsync();
            Order orderObj = new Order();

            if(await reader.ReadAsync())
            {
                orderObj = MapOrder(reader);
            }

            return orderObj;
        }

        public async Task<OrderItem> CreateOrderItemAsync(OrderItem item)
        {
            string query = @"insert into order_items (order_id, product_id, quantity, unit_price, total_price,
discount_amount, created_at, updated_at, product_name) values (@OrderId, @ProductId, @Quantity, @UnitPrice,
@TotalPrice, @DiscountAmount, @CreatedAt, @UpdatedAt, @ProductName) Returning *;";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(query, connection);

            AddOrderItemParameters(command, item);

            var reader = await command.ExecuteReaderAsync();
            OrderItem orderItem = new OrderItem();

            if(await reader.ReadAsync())
            {
                orderItem = MapOrderItem(reader);
            }

            return orderItem;
        }

        public async Task<Order?> GetOrderByReferenceNumberAsync(string referenceNumber)
        {
            string query = @"select o.*
                             from Orders o
                            where o.reference_number = @ReferenceNumber;";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@ReferenceNumber", referenceNumber);

            Order order = new Order();

            var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                order = MapOrder(reader);
            }

            return order;
        }

        public async Task<List<Order>> GetOrdersByStatusAsync(OrderStatus status, int page = 1, int pageSize = 10)
        {
            string query = @"select * from orders where Status = @OrderStatus offset @offset limit @pageSize;";

            int offset = (page - 1) * pageSize;

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@offset", offset);
            command.Parameters.AddWithValue("@pageSize", pageSize);
            command.Parameters.AddWithValue("@OrderStatus", status);

            List<Order> orders = new List<Order>();

            var reader = await command.ExecuteReaderAsync();

            while(await reader.ReadAsync())
            {
                orders.Add(MapOrder(reader));
            }

            return orders;
        }

        public async Task<bool> UpdateOrderPaymentStatusAsync(int orderId, PaymentStatus paymentStatus)
        {
            string query = @"Update Orders
set is_paid = case when @paymentStatus = @completedStatus then 1 else 0 end
where id = @orderId;";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@paymentStatus", paymentStatus);
            command.Parameters.AddWithValue("@completedStatus", PaymentStatus.Completed);

            int rowEffected = await command.ExecuteNonQueryAsync();

            return rowEffected > 0;
        }

        public async Task<Order?> GetOrderByCartIdAsync(int cartId)
        {
            string query = @"select o.*
                             from Orders o
                            where o.cart_id = @cartId;";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@cartId", cartId);

            Order order = new Order();

            var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                order = MapOrder(reader);
            }

            return order;
        }

        public async Task<bool> UpdateOrderAsync(Order order)
        {
            string query = @"Update Orders
set user_id = @userId,
cart_id = @cartId,
status = @status,
sub_total = @subTotal,
tax_amount = @taxAmount,
shipping_amount = @shippingAmount,
total = @total,
discount_total = @discountTotal,
customer_email = @customerEmail,
customer_phone = @customerPhone,
reference_number = @referenceNumber,
completed_at = @completedAt,
cancelled_at = @cancelledAt,
shipping_name = @shippingName,
shipping_address1 = @shippingAddress1,
shipping_address2 = @shippingAddress2,
shipping_city = @shippingCity,
shipping_state = @shippingState,
shipping_postal_code = @shippingPostalCode,
shipping_country = @shippingCountry,
created_at = @createdAt,
updated_at = @updatedAt,
is_paid = @isPaid,
session_id = @sessionId
where id = @orderId;";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(query, connection);

            AddOrderParameters(command, order);
            command.Parameters.AddWithValue("@orderId", order.Id);

            int rowEffected = await command.ExecuteNonQueryAsync();

            return rowEffected > 0;
        }

        public async Task<bool> DeleteOrderItemsAsync(int orderId)
        {
            string query = @"Delete from order_items oi where oi.order_id = @orderId;";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@orderId", orderId);

            int rowEffected = await command.ExecuteNonQueryAsync();

            await connection.CloseAsync();

            return rowEffected > 0;
        }

        public async Task<bool> UserHasPaidOrderAsync(int userId)
        {
            string query = @"SELECT CASE 
    WHEN EXISTS (
        SELECT 1 
        FROM Orders 
        WHERE userId = @userId AND isPaid = 1
    ) THEN 1 
    ELSE 0 
END";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@orderId", userId);

            object result = await command.ExecuteScalarAsync();

            await connection.CloseAsync();

            return Convert.ToBoolean(result);
        }

        private async Task<NpgsqlConnection> OpenConnectionAsync()
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }
        

        private Order MapOrder(NpgsqlDataReader reader)
        {
            return new Order()
            {
                Id = reader.GetInt16(reader.GetOrdinal("id")),
                UserId = reader.GetInt16(reader.GetOrdinal("user_id")),
                CartId = reader.GetInt16(reader.GetOrdinal("cart_id")),
                IsPaid = reader.GetBoolean(reader.GetOrdinal("is_paid")),
                Status = reader.GetInt16(reader.GetOrdinal("status")),
                SubTotal = reader.GetDecimal(reader.GetOrdinal("subtotal")),
                TaxAmount = reader.GetDecimal(reader.GetOrdinal("tax_amount")),
                ShippingAmount = reader.GetDecimal(reader.GetOrdinal("shipping_amount")),
                Total = reader.GetDecimal(reader.GetOrdinal("total")),
                DiscountTotal = reader.GetDecimal(reader.GetOrdinal("discount_total")),
                CustomerEmail = reader.GetString(reader.GetOrdinal("customer_email")),
                CustomerPhone = reader.GetString(reader.GetOrdinal("customer_phone")),
                CompletedAt = reader.GetDateTime(reader.GetOrdinal("completed_at")),
                CancelledAt = reader.GetDateTime(reader.GetOrdinal("cancelled_at")),
                ShippingName = reader.GetString(reader.GetOrdinal("shipping_name")),
                ShippingAddress1 = reader.GetString(reader.GetOrdinal("shipping_address1")),
                ShippingAddress2 = reader.GetString(reader.GetOrdinal("shipping_address2")),
                ShippingCity = reader.GetString(reader.GetOrdinal("shipping_city")),
                ShippingState = reader.GetString(reader.GetOrdinal("shipping_state")),
                ShippingPostalCode = reader.GetString(reader.GetOrdinal("shipping_postal_code")),
                ShippingCountry = reader.GetString(reader.GetOrdinal("shipping_country")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
            };
        }

        private OrderItem MapOrderItem(NpgsqlDataReader reader)
        {
            return new OrderItem()
            {
                Id = reader.GetInt16(reader.GetOrdinal("id")),
                OrderId = reader.GetInt16(reader.GetOrdinal("order_id")),
                ProductId = reader.GetInt16(reader.GetOrdinal("product_id")),
                ProductName = reader.GetString(reader.GetOrdinal("product_name")),
                Quantity = reader.GetInt16(reader.GetOrdinal("quantity")),
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
            command.Parameters.AddWithValue("@Status", order.Status);
            command.Parameters.AddWithValue("@SubTotal", order.SubTotal);
            command.Parameters.AddWithValue("@TaxAmount", order.TaxAmount);
            command.Parameters.AddWithValue("@ShippingAmount", order.ShippingAmount);
            command.Parameters.AddWithValue("@Total", order.Total);
            command.Parameters.AddWithValue("@DiscountTotal", order.DiscountTotal);
            command.Parameters.AddWithValue("@CustomerEmail", order.CustomerEmail);
            command.Parameters.AddWithValue("@CustomerPhone", order.CustomerPhone);
            command.Parameters.AddWithValue("@ReferenceNumber", order.ReferenceNumber);
            command.Parameters.AddWithValue("@CompletedAt", order.CompletedAt);
            command.Parameters.AddWithValue("@CancelledAt", order.CancalledAt);
            command.Parameters.AddWithValue("@ShippingName", order.ShippingName);
            command.Parameters.AddWithValue("@ShippingAddress1", order.ShippingAddress1);
            command.Parameters.AddWithValue("@ShippingAddress2", order.ShippingAddress2);
            command.Parameters.AddWithValue("@ShippingCity", order.ShippingCity);
            command.Parameters.AddWithValue("@ShippingState", order.ShippingState);
            command.Parameters.AddWithValue("@ShippingPostalCode", order.ShippingPostalCode);
            command.Parameters.AddWithValue("@ShippingCountry", order.ShippingCountry);
            command.Parameters.AddWithValue("@CreatedAt", order.CreatedAt);
            command.Parameters.AddWithValue("@UpdatedAt", order.UpdatedAt);
            command.Parameters.AddWithValue("@IsPaid", order.isPaid);
            command.Parameters.AddWithValue("@SessionId", order.sessionId);
        }

        private void AddOrderItemParameters(NpgsqlCommand command, OrderItem orderItem)
        {
            command.Parameters.AddWithValue("@OrderId", item.OrderId);
            command.Parameters.AddWithValue("@ProductId", item.ProductId);
            command.Parameters.AddWithValue("@Quantity", item.Quantity);
            command.Parameters.AddWithValue("@UnitPrice", item.UnitPrice);
            command.Parameters.AddWithValue("@TotalPrice", item.TotalPrice);
            command.Parameters.AddWithValue("@DiscountAmount", item.DiscountAmount);
            command.Parameters.AddWithValue("@CreatedAt", item.CreatedAt);
            command.Parameters.AddWithValue("@UpdatedAt", item.UpdatedAt);
            command.Parameters.AddWithValue("@ProductName", item.ProductName);
        }
    }
}