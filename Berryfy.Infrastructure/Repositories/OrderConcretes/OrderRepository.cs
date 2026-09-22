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
            string query = @"select o.*, oi.Quantitiy as OI_Quantity, oi.UnitPrice as OI_UnitPrice, 
                             oi.TotalPrice as OI_TotalPrice, oi.DiscountAmount as OI_DiscountAmount,
                             p.Id as P_Id, p.ProductName as P_ProductName, p.Description as P_Description,
                             p.StockQuantity as P_StockQuantity, p.ImageUrl as P_ImageUrl,
                             p.Price as P_Price, p.ReservedStock as P_ReservedStock, 
                             p.LowStockThreshold as P_LowStockThreshold, p.IsActive as P_IsActive, p.SKU as P_SKU 
                             from Orders o
                            Left join OrderItems oi on (oi.OrderId = o.Id)
                            leff join Products p on (oi.productId = p.Id)
                            where o.Id = @OrderId
                            limit 1;";

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@OrderId", orderId);

            Order order = null;

            var reader = await command.ExecuteReaderAsync();

            while(await reader.ReadAsync())
            {
                if(order == null)
                {
                    order = new Order
                    {
                        Id = reader.GetInt16(reader.GetOrdinal("Id")),
                        UserId = reader.GetInt16(reader.GetOrdinal("UserId")),
                        CartId = reader.GetInt16(reader.GetOrdinal("CartId")),
                        Status = (OrderStatus)reader.GetValue(reader.GetOrdinal("Status")),
                        SubTotal = reader.GetDecimal(reader.GetOrdinal("SubTotal")),
                        TaxAmount = reader.GetDecimal(reader.GetOrdinal("TaxAmount")),
                        ShippingAmount = reader.GetDecimal(reader.GetOrdinal("ShippingAmount")),
                        Total = reader.GetDecimal(reader.GetOrdinal("Total")),
                        DiscountTotal = reader.GetDecimal(reader.GetOrdinal("DiscountTotal")),
                        CustomerEmail = reader.GetString(reader.GetOrdinal("CustomerEmail")),
                        CustomerPhone = reader.GetString(reader.GetOrdinal("CustomerPhone")),
                        ReferenceNumber = reader.GetString(reader.GetOrdinal("ReferenceNumber")),
                        CompletedAt = reader.GetDateTime(reader.GetOrdinal("CompletedAt")),
                        CancalledAt = reader.GetDateTime(reader.GetOrdinal("CancelledAt")),
                        ShippingName = reader.GetString(reader.GetOrdinal("ShippingName")),
                        ShippingAddress1 = reader.GetString(reader.GetOrdinal("ShippingAddress1")),
                        ShippingAddress2 = reader.GetString(reader.GetOrdinal("ShippingAddress2")),
                        ShippingCity = reader.GetString(reader.GetOrdinal("ShippingCity")),
                        ShippingCountry = reader.GetString(reader.GetOrdinal("ShippingCountry")),
                        ShippingPostalCode = reader.GetString(reader.GetOrdinal("ShippingPostalCode")),
                        ShippingState = reader.GetString(reader.GetOrdinal("ShippingState")),
                        isPaid = reader.GetBoolean(reader.GetOrdinal("IsPaid")),
                        sessionId = reader.GetString(reader.GetOrdinal("SessionId")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),

                        OrderItems = new List<OrderItem>()
                    };

                    var orderItem = new OrderItem
                    {
                        Id = reader.GetInt16(reader.GetOrdinal("OI_Id")),
                        ProductId = reader.GetInt16(reader.GetOrdinal("OI_ProductId")),
                        Quantity = reader.GetInt32(reader.GetOrdinal("OI_Quantity")),
                        UnitPrice = reader.GetDecimal(reader.GetOrdinal("OI_UnitPrice")),
                        TotalPrice = reader.GetDecimal(reader.GetOrdinal("OI_TotalPrice")),
                        DiscountAmount = reader.GetDecimal(reader.GetOrdinal("OI_DiscountAmount")),

                        Product = new Product
                        {
                            Id = reader.GetInt16(reader.GetOrdinal("P_Id")),
                            Name = reader.GetString(reader.GetOrdinal("P_ProductName")),
                            Description = reader.GetString(reader.GetOrdinal("P_Description")),
                            StockQuantity = reader.GetInt32(reader.GetOrdinal("P_StockQuantity")),
                            ImageUrl = reader.GetString(reader.GetOrdinal("P_ImageUrl")),
                            Price = reader.GetDecimal(reader.GetOrdinal("P_Price")),
                            ReservedStock = reader.GetInt32(reader.GetOrdinal("P_ReservedStock")),
                            LowStockThreshold = reader.GetInt32(reader.GetOrdinal("P_LowStockThreshold")),
                            IsActive = reader.GetBoolean(reader.GetOrdinal("P_IsActive")),
                            SKU = reader.GetString(reader.GetOrdinal("P_SKU"))
                        }
                    };

                    order.OrderItems.Add(orderItem);
                }
            }

            return order;
        }

        public async Task<List<Order>> GetUserOrdersAsync(int userId, int page = 1, int pageSize = 10)
        {
            string query = @"select o.*, oi.Quantitiy as OI_Quantity, oi.UnitPrice as OI_UnitPrice, 
                             oi.TotalPrice as OI_TotalPrice, oi.DiscountAmount as OI_DiscountAmount,
                             p.Id as P_Id, p.ProductName as P_ProductName, p.Description as P_Description,
                             p.StockQuantity as P_StockQuantity, p.ImageUrl as P_ImageUrl,
                             p.Price as P_Price, p.ReservedStock as P_ReservedStock, 
                             p.LowStockThreshold as P_LowStockThreshold, p.IsActive as P_IsActive, p.SKU as P_SKU 
                             from Orders o
                            Left join OrderItems oi on (oi.OrderId = o.Id)
                            inner join Products p on (oi.productId = p.Id)
                            where o.UserId = @UserId
                            order by createdAt desc
                            offset @offset
                            limit @pageSize;";

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var offset = (page - 1) * pageSize;

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@offset", offset);
            command.Parameters.AddWithValue("@pageSize", pageSize);

            Dictionary<int, Order> orders = new Dictionary<int, Order>();

            var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                int orderId = reader.GetInt16(reader.GetOrdinal("Id"));

                if (!orders.TryGetValue(orderId, out var order))
                {
                    order = new Order
                    {
                        Id = reader.GetInt16(reader.GetOrdinal("Id")),
                        UserId = reader.GetInt16(reader.GetOrdinal("UserId")),
                        CartId = reader.GetInt16(reader.GetOrdinal("CartId")),
                        Status = (OrderStatus)reader.GetValue(reader.GetOrdinal("Status")),
                        SubTotal = reader.GetDecimal(reader.GetOrdinal("SubTotal")),
                        TaxAmount = reader.GetDecimal(reader.GetOrdinal("TaxAmount")),
                        ShippingAmount = reader.GetDecimal(reader.GetOrdinal("ShippingAmount")),
                        Total = reader.GetDecimal(reader.GetOrdinal("Total")),
                        DiscountTotal = reader.GetDecimal(reader.GetOrdinal("DiscountTotal")),
                        CustomerEmail = reader.GetString(reader.GetOrdinal("CustomerEmail")),
                        CustomerPhone = reader.GetString(reader.GetOrdinal("CustomerPhone")),
                        ReferenceNumber = reader.GetString(reader.GetOrdinal("ReferenceNumber")),
                        CompletedAt = reader.GetDateTime(reader.GetOrdinal("CompletedAt")),
                        CancalledAt = reader.GetDateTime(reader.GetOrdinal("CancelledAt")),
                        ShippingName = reader.GetString(reader.GetOrdinal("ShippingName")),
                        ShippingAddress1 = reader.GetString(reader.GetOrdinal("ShippingAddress1")),
                        ShippingAddress2 = reader.GetString(reader.GetOrdinal("ShippingAddress2")),
                        ShippingCity = reader.GetString(reader.GetOrdinal("ShippingCity")),
                        ShippingCountry = reader.GetString(reader.GetOrdinal("ShippingCountry")),
                        ShippingPostalCode = reader.GetString(reader.GetOrdinal("ShippingPostalCode")),
                        ShippingState = reader.GetString(reader.GetOrdinal("ShippingState")),
                        isPaid = reader.GetBoolean(reader.GetOrdinal("IsPaid")),
                        sessionId = reader.GetString(reader.GetOrdinal("SessionId")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                        OrderItems = new List<OrderItem>()

                    };
                    orders.Add(orderId, order);
                }

                if (!reader.IsDBNull(reader.GetOrdinal("OI_Id")))
                {

                    var orderItem = new OrderItem
                    {
                        Id = reader.GetInt16(reader.GetOrdinal("OI_Id")),
                        ProductId = reader.GetInt16(reader.GetOrdinal("OI_ProductId")),
                        Quantity = reader.GetInt32(reader.GetOrdinal("OI_Quantity")),
                        UnitPrice = reader.GetDecimal(reader.GetOrdinal("OI_UnitPrice")),
                        TotalPrice = reader.GetDecimal(reader.GetOrdinal("OI_TotalPrice")),
                        DiscountAmount = reader.GetDecimal(reader.GetOrdinal("OI_DiscountAmount"))
                    };

                    if (!reader.IsDBNull(reader.GetOrdinal("P_Id")))
                    {

                        orderItem.Product = new Product
                        {
                            Id = reader.GetInt16(reader.GetOrdinal("P_Id")),
                            Name = reader.GetString(reader.GetOrdinal("P_ProductName")),
                            Description = reader.GetString(reader.GetOrdinal("P_Description")),
                            StockQuantity = reader.GetInt32(reader.GetOrdinal("P_StockQuantity")),
                            ImageUrl = reader.GetString(reader.GetOrdinal("P_ImageUrl")),
                            Price = reader.GetDecimal(reader.GetOrdinal("P_Price")),
                            ReservedStock = reader.GetInt32(reader.GetOrdinal("P_ReservedStock")),
                            LowStockThreshold = reader.GetInt32(reader.GetOrdinal("P_LowStockThreshold")),
                            IsActive = reader.GetBoolean(reader.GetOrdinal("P_IsActive")),
                            SKU = reader.GetString(reader.GetOrdinal("P_SKU"))
                        };

                    }
                    order.OrderItems.Add(orderItem);
                }
            }
            return orders.Values.ToList();
        }

        public async Task<List<Order>> GetAllOrdersAsync(int page = 1, int pageSize = 50)
        {
            string query = @"SELECT
    o.Id AS OrderId,
    o.UserId AS OrderUserId,
    o.CartId AS OrderCartId,
    o.Status AS OrderStatus,
    o.SubTotal AS OrderSubTotal,
    o.TaxAmount AS OrderTaxAmount,
    o.ShippingAmount AS OrderShippingAmount,
    o.Total AS OrderTotal,
    o.DiscountTotal AS OrderDiscountTotal,
    o.CustomerEmail AS OrderCustomerEmail,
    o.CustomerPhone AS OrderCustomerPhone,
    o.ReferenceNumber AS OrderReferenceNumber,
    o.CompletedAt AS OrderCompletedAt,
    o.CancelledAt AS OrderCancelledAt,
    o.ShippingName AS OrderShippingName,
    o.ShippingAddress1 AS OrderShippingAddress1,
    o.ShippingAddress2 AS OrderShippingAddress2,
    o.ShippingCity AS OrderShippingCity,
    o.ShippingState AS OrderShippingState,
    o.ShippingPostalCode AS OrderShippingPostalCode,
    o.ShippingCountry AS OrderShippingCountry,
    o.CreatedAt AS OrderCreatedAt,
    o.UpdatedAt AS OrderUpdatedAt,
    o.IsPaid AS OrderIsPaid,
    o.SessionId AS OrderSessionId,

    oi.Id AS OrderItemId,
    oi.Quantity AS ItemQuantity,
    oi.UnitPrice AS ItemUnitPrice,
    oi.TotalPrice AS ItemTotalPrice,
    oi.DiscountAmount AS ItemDiscountAmount,

    p.Id AS ProductId,
    p.Name AS ProductName,
    p.Description AS ProductDescription,
    p.StockQuantity AS ProductStockQuantity,
    p.ImageUrl AS ProductImageUrl,
    p.Price AS ProductPrice,
    p.ReservedStock AS ProductReservedStock,
    p.LowStockThreshold AS ProductLowStockThreshold,
    p.IsActive AS ProductIsActive,
    p.SKU AS ProductSKU,

    u.Id AS UserId,
    u.FirstName AS UserFirstName,
    u.LastName AS UserLastName

FROM (
    SELECT *
    FROM Orders
    ORDER BY CreatedAt DESC
    OFFSET @offset
    LIMIT @pageSize
) o
LEFT JOIN OrderItems oi ON o.Id = oi.OrderId
LEFT JOIN Products p ON oi.ProductId = p.Id
LEFT JOIN AspNetUsers u ON o.UserId = u.Id
ORDER BY o.CreatedAt DESC, o.Id;";


            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(query, connection);

            int offset = (page - 1) * pageSize;

            command.Parameters.AddWithValue("@offset", offset);
            command.Parameters.AddWithValue("@pageSize", pageSize);


            var reader = await command.ExecuteReaderAsync();

            Dictionary<int, Order> orders = new Dictionary<int, Order>();
            Dictionary<int, User> users = new Dictionary<int, User>();

            while(await reader.ReadAsync())
            {
                int orderId = reader.GetInt16(reader.GetOrdinal("OrderId"));
                int userId = reader.GetInt16(reader.GetOrdinal("UserId"));

                if (!orders.TryGetValue(orderId, out var order))
                {
                    order = new Order
                    {
                        Id = reader.GetInt16(reader.GetOrdinal("Id")),
                        UserId = reader.GetInt16(reader.GetOrdinal("UserId")),
                        CartId = reader.GetInt16(reader.GetOrdinal("CartId")),
                        Status = (OrderStatus)reader.GetValue(reader.GetOrdinal("Status")),
                        SubTotal = reader.GetDecimal(reader.GetOrdinal("SubTotal")),
                        TaxAmount = reader.GetDecimal(reader.GetOrdinal("TaxAmount")),
                        ShippingAmount = reader.GetDecimal(reader.GetOrdinal("ShippingAmount")),
                        Total = reader.GetDecimal(reader.GetOrdinal("Total")),
                        DiscountTotal = reader.GetDecimal(reader.GetOrdinal("DiscountTotal")),
                        CustomerEmail = reader.GetString(reader.GetOrdinal("CustomerEmail")),
                        CustomerPhone = reader.GetString(reader.GetOrdinal("CustomerPhone")),
                        ReferenceNumber = reader.GetString(reader.GetOrdinal("ReferenceNumber")),
                        CompletedAt = reader.GetDateTime(reader.GetOrdinal("CompletedAt")),
                        CancalledAt = reader.GetDateTime(reader.GetOrdinal("CancelledAt")),
                        ShippingName = reader.GetString(reader.GetOrdinal("ShippingName")),
                        ShippingAddress1 = reader.GetString(reader.GetOrdinal("ShippingAddress1")),
                        ShippingAddress2 = reader.GetString(reader.GetOrdinal("ShippingAddress2")),
                        ShippingCity = reader.GetString(reader.GetOrdinal("ShippingCity")),
                        ShippingCountry = reader.GetString(reader.GetOrdinal("ShippingCountry")),
                        ShippingPostalCode = reader.GetString(reader.GetOrdinal("ShippingPostalCode")),
                        ShippingState = reader.GetString(reader.GetOrdinal("ShippingState")),
                        isPaid = reader.GetBoolean(reader.GetOrdinal("IsPaid")),
                        sessionId = reader.GetString(reader.GetOrdinal("SessionId")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                        OrderItems = new List<OrderItem>()

                    };
                    orders.Add(orderId, order);
                }

                if (!users.TryGetValue(userId, out var user))
                {
                    user = new User
                    {
                        FirstName = reader.GetString(reader.GetOrdinal("UserFirstName")),
                        Id = reader.GetInt16(reader.GetOrdinal("UserId")),
                        LastName = reader.GetString(reader.GetOrdinal("UserLastName"))
                    };

                    users.Add(userId, user);
                }

                if (!reader.IsDBNull(reader.GetOrdinal("OrderItemId")))
                {

                    var orderItem = new OrderItem
                    {
                        Id = reader.GetInt16(reader.GetOrdinal("OrderItemId")),
                        Quantity = reader.GetInt32(reader.GetOrdinal("ItemQuantity")),
                        UnitPrice = reader.GetDecimal(reader.GetOrdinal("ItemUnitPrice")),
                        TotalPrice = reader.GetDecimal(reader.GetOrdinal("ItemTotalPrice")),
                        DiscountAmount = reader.GetDecimal(reader.GetOrdinal("ItemDiscountAmount"))
                    };

                    if (!reader.IsDBNull(reader.GetOrdinal("ProductId")))
                    {

                        orderItem.Product = new Product
                        {
                            Id = reader.GetInt16(reader.GetOrdinal("ProductId")),
                            Name = reader.GetString(reader.GetOrdinal("ProductName")),
                            Description = reader.GetString(reader.GetOrdinal("ProductDescription")),
                            StockQuantity = reader.GetInt32(reader.GetOrdinal("ProductStockQuantity")),
                            ImageUrl = reader.GetString(reader.GetOrdinal("ProductImageUrl")),
                            Price = reader.GetDecimal(reader.GetOrdinal("ProductPrice")),
                            ReservedStock = reader.GetInt32(reader.GetOrdinal("ProductReservedStock")),
                            LowStockThreshold = reader.GetInt32(reader.GetOrdinal("ProductLowStockThreshold")),
                            IsActive = reader.GetBoolean(reader.GetOrdinal("ProductIsActive")),
                            SKU = reader.GetString(reader.GetOrdinal("ProductSKU"))
                        };

                    }
                    order.OrderItems.Add(orderItem);
                    order.User = user;
                }
            }

            return orders.Values.ToList();

        }


        public async Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus)
        {
            string query = @"Update Orders
                            Set Status = @newStatus,
                            UpdatedAt = 
                            CompletedAt = Case When @newStatus = @completedAtStatus Then CURRENT_TIMESTAMP else CompletedAt End,
                            CancelledAt = Case When @newStatus = @cancelledAtStatus Then CURRENT_TIMESTAMP else CancelledAt End
                            Where Id = @orderId;";

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

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
            string query = @"Insert into Orders (UserId, CartId, Status, SubTotal, TaxAmount, ShippingAmount,
Total, DiscountTotal, CustomerEmail, CustomerPhone, ReferenceNumber, CompletedAt, CancelledAt, ShippingName,
ShippingAddress1, ShippingAddress2, ShippingCity, ShippingState, ShippingPostalCode, ShippingCountry, 
CreatedAt, UpdatedAt, isPaid, sessionId) Values (@UserId, @CartId, @Status, @SubTotal, @TaxAmount, @ShippingAmount,
@Total, @DiscountTotal, @CustomerEmail, @CustomerPhone, @ReferenceNumber, @CompletedAt, @CancelledAt, @ShippingName,
@ShippingAddress1, @ShippingAddress2, @ShippingCity, @ShippingState, @ShippingPostalCode, @ShippingCountry, 
@CreatedAt, @UpdatedAt, @isPaid, @sessionId) Returning *;";

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(query, connection);

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
            command.Parameters.AddWithValue("@ShippintCountry", order.ShippingCountry);
            command.Parameters.AddWithValue("@CreatedAt", order.CreatedAt);
            command.Parameters.AddWithValue("@UpdatedAt", order.UpdatedAt);
            command.Parameters.AddWithValue("@isPaid", order.isPaid);
            command.Parameters.AddWithValue("@sessionId", order.sessionId);

            var reader = await command.ExecuteReaderAsync();
            Order orderObj = null;

            while(await reader.ReadAsync())
            {
                if(orderObj == null)
                {
                    orderObj = new Order
                    {
                        Id = reader.GetInt16(reader.GetOrdinal("Id")),
                        UserId = reader.GetInt16(reader.GetOrdinal("UserId")),
                        CartId = reader.GetInt16(reader.GetOrdinal("CartId")),
                        Status = (OrderStatus)reader.GetValue(reader.GetOrdinal("Status")),
                        SubTotal = reader.GetDecimal(reader.GetOrdinal("SubTotal")),
                        TaxAmount = reader.GetDecimal(reader.GetOrdinal("TaxAmount")),
                        ShippingAmount = reader.GetDecimal(reader.GetOrdinal("ShippingAmount")),
                        Total = reader.GetDecimal(reader.GetOrdinal("Total")),
                        DiscountTotal = reader.GetDecimal(reader.GetOrdinal("DiscountTotal")),
                        CustomerEmail = reader.GetString(reader.GetOrdinal("CustomerEmail")),
                        CustomerPhone = reader.GetString(reader.GetOrdinal("CustomerPhone")),
                        ReferenceNumber = reader.GetString(reader.GetOrdinal("ReferenceNumber")),
                        CompletedAt = reader.GetDateTime(reader.GetOrdinal("CompletedAt")),
                        CancalledAt = reader.GetDateTime(reader.GetOrdinal("CancelledAt")),
                        ShippingName = reader.GetString(reader.GetOrdinal("ShippingName")),
                        ShippingAddress1 = reader.GetString(reader.GetOrdinal("ShippingAddress1")),
                        ShippingAddress2 = reader.GetString(reader.GetOrdinal("ShippingAddress2")),
                        ShippingCity = reader.GetString(reader.GetOrdinal("ShippingCity")),
                        ShippingCountry = reader.GetString(reader.GetOrdinal("ShippingCountry")),
                        ShippingPostalCode = reader.GetString(reader.GetOrdinal("ShippingPostalCode")),
                        ShippingState = reader.GetString(reader.GetOrdinal("ShippingState")),
                        isPaid = reader.GetBoolean(reader.GetOrdinal("IsPaid")),
                        sessionId = reader.GetString(reader.GetOrdinal("SessionId")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                        OrderItems = new List<OrderItem>()
                    };
                }
            }

            return orderObj;
        }

        public async Task<OrderItem> CreateOrderItemAsync(OrderItem item)
        {
            string query = @"insert into OrderItems (OrderId, ProductId, Quantity, UnitPrice, TotalPrice,
DiscountAmount, CreatedAt, UpdatedAt, ProductName) values (@OrderId, @ProductId, @Quantity, @UnitPrice,
@TotalPrice, @DiscountAmount, @CreatedAt, @UpdatedAt, @ProductName) Returning *;";

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@OrderId", item.OrderId);
            command.Parameters.AddWithValue("@ProductId", item.ProductId);
            command.Parameters.AddWithValue("@Quantity", item.Quantity);
            command.Parameters.AddWithValue("@UnitPrice", item.UnitPrice);
            command.Parameters.AddWithValue("@TotalPrice", item.TotalPrice);
            command.Parameters.AddWithValue("@DiscountAmount", item.DiscountAmount);
            command.Parameters.AddWithValue("@CreatedAt", item.CreatedAt);
            command.Parameters.AddWithValue("@UpdatedAt", item.UpdatedAt);
            command.Parameters.AddWithValue("@ProductName", item.ProductName);

            var reader = await command.ExecuteReaderAsync();
            OrderItem orderItem = null;

            while(await reader.ReadAsync())
            {
                if(orderItem == null)
                {
                    orderItem = new OrderItem
                    {
                        OrderId = reader.GetInt16(reader.GetOrdinal("OrderId")),
                        ProductId = reader.GetInt16(reader.GetOrdinal("ProductId")),
                        Quantity = reader.GetInt16(reader.GetOrdinal("Quantity")),
                        UnitPrice = reader.GetInt16(reader.GetOrdinal("UnitPrice")),
                        TotalPrice = reader.GetDecimal(reader.GetOrdinal("TotalPrice")),
                        DiscountAmount = reader.GetDecimal(reader.GetOrdinal("DiscountAmount")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                        ProductName = reader.GetString(reader.GetOrdinal("ProductName"))
                    };
                }
            }

            return orderItem;
        }

        public async Task<Order?> GetOrderByReferenceNumberAsync(string referenceNumber)
        {
            string query = @"select o.*, oi.Quantitiy as OI_Quantity, oi.UnitPrice as OI_UnitPrice, 
                             oi.TotalPrice as OI_TotalPrice, oi.DiscountAmount as OI_DiscountAmount,
                             p.Id as P_Id, p.ProductName as P_ProductName, p.Description as P_Description,
                             p.StockQuantity as P_StockQuantity, p.ImageUrl as P_ImageUrl,
                             p.Price as P_Price, p.ReservedStock as P_ReservedStock, 
                             p.LowStockThreshold as P_LowStockThreshold, p.IsActive as P_IsActive, p.SKU as P_SKU 
                             from Orders o
                            Left join OrderItems oi on (oi.OrderId = o.Id)
                            leff join Products p on (oi.productId = p.Id)
                            where o.ReferenceNumber = @referenceNumber
                            limit 1;";

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@referenceNumber", referenceNumber);

            Order order = null;

            var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                if (order == null)
                {
                    order = new Order
                    {
                        Id = reader.GetInt16(reader.GetOrdinal("Id")),
                        UserId = reader.GetInt16(reader.GetOrdinal("UserId")),
                        CartId = reader.GetInt16(reader.GetOrdinal("CartId")),
                        Status = (OrderStatus)reader.GetValue(reader.GetOrdinal("Status")),
                        SubTotal = reader.GetDecimal(reader.GetOrdinal("SubTotal")),
                        TaxAmount = reader.GetDecimal(reader.GetOrdinal("TaxAmount")),
                        ShippingAmount = reader.GetDecimal(reader.GetOrdinal("ShippingAmount")),
                        Total = reader.GetDecimal(reader.GetOrdinal("Total")),
                        DiscountTotal = reader.GetDecimal(reader.GetOrdinal("DiscountTotal")),
                        CustomerEmail = reader.GetString(reader.GetOrdinal("CustomerEmail")),
                        CustomerPhone = reader.GetString(reader.GetOrdinal("CustomerPhone")),
                        ReferenceNumber = reader.GetString(reader.GetOrdinal("ReferenceNumber")),
                        CompletedAt = reader.GetDateTime(reader.GetOrdinal("CompletedAt")),
                        CancalledAt = reader.GetDateTime(reader.GetOrdinal("CancelledAt")),
                        ShippingName = reader.GetString(reader.GetOrdinal("ShippingName")),
                        ShippingAddress1 = reader.GetString(reader.GetOrdinal("ShippingAddress1")),
                        ShippingAddress2 = reader.GetString(reader.GetOrdinal("ShippingAddress2")),
                        ShippingCity = reader.GetString(reader.GetOrdinal("ShippingCity")),
                        ShippingCountry = reader.GetString(reader.GetOrdinal("ShippingCountry")),
                        ShippingPostalCode = reader.GetString(reader.GetOrdinal("ShippingPostalCode")),
                        ShippingState = reader.GetString(reader.GetOrdinal("ShippingState")),
                        isPaid = reader.GetBoolean(reader.GetOrdinal("IsPaid")),
                        sessionId = reader.GetString(reader.GetOrdinal("SessionId")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),

                        OrderItems = new List<OrderItem>()
                    };

                    var orderItem = new OrderItem
                    {
                        Id = reader.GetInt16(reader.GetOrdinal("OI_Id")),
                        ProductId = reader.GetInt16(reader.GetOrdinal("OI_ProductId")),
                        Quantity = reader.GetInt32(reader.GetOrdinal("OI_Quantity")),
                        UnitPrice = reader.GetDecimal(reader.GetOrdinal("OI_UnitPrice")),
                        TotalPrice = reader.GetDecimal(reader.GetOrdinal("OI_TotalPrice")),
                        DiscountAmount = reader.GetDecimal(reader.GetOrdinal("OI_DiscountAmount")),

                        Product = new Product
                        {
                            Id = reader.GetInt16(reader.GetOrdinal("P_Id")),
                            Name = reader.GetString(reader.GetOrdinal("P_ProductName")),
                            Description = reader.GetString(reader.GetOrdinal("P_Description")),
                            StockQuantity = reader.GetInt32(reader.GetOrdinal("P_StockQuantity")),
                            ImageUrl = reader.GetString(reader.GetOrdinal("P_ImageUrl")),
                            Price = reader.GetDecimal(reader.GetOrdinal("P_Price")),
                            ReservedStock = reader.GetInt32(reader.GetOrdinal("P_ReservedStock")),
                            LowStockThreshold = reader.GetInt32(reader.GetOrdinal("P_LowStockThreshold")),
                            IsActive = reader.GetBoolean(reader.GetOrdinal("P_IsActive")),
                            SKU = reader.GetString(reader.GetOrdinal("P_SKU"))
                        }
                    };

                    order.OrderItems.Add(orderItem);
                }
            }

            return order;

        }

        public async Task<List<Order>> GetOrdersByStatusAsync(OrderStatus status, int page = 1, int pageSize = 10)
        {
            string query = @"select * from orders where Status = @orderStatus offset @offset limit @pageSize;";

            int offset = (page - 1) * pageSize;

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@offset", offset);
            command.Parameters.AddWithValue("@pageSize", pageSize);
            command.Parameters.AddWithValue("orderStatus", status);

            List<Order> orders = new List<Order>();

            var reader = await command.ExecuteReaderAsync();

            while(await reader.ReadAsync())
            {
                orders.Add(new Order
                {
                    Id = reader.GetInt16(reader.GetOrdinal("Id")),
                    UserId = reader.GetInt16(reader.GetOrdinal("UserId")),
                    CartId = reader.GetInt16(reader.GetOrdinal("CartId")),
                    Status = (OrderStatus)reader.GetValue(reader.GetOrdinal("Status")),
                    SubTotal = reader.GetDecimal(reader.GetOrdinal("SubTotal")),
                    TaxAmount = reader.GetDecimal(reader.GetOrdinal("TaxAmount")),
                    ShippingAmount = reader.GetDecimal(reader.GetOrdinal("ShippingAmount")),
                    Total = reader.GetDecimal(reader.GetOrdinal("Total")),
                    DiscountTotal = reader.GetDecimal(reader.GetOrdinal("DiscountTotal")),
                    CustomerEmail = reader.GetString(reader.GetOrdinal("CustomerEmail")),
                    CustomerPhone = reader.GetString(reader.GetOrdinal("CustomerPhone")),
                    ReferenceNumber = reader.GetString(reader.GetOrdinal("ReferenceNumber")),
                    CompletedAt = reader.GetDateTime(reader.GetOrdinal("CompletedAt")),
                    CancalledAt = reader.GetDateTime(reader.GetOrdinal("CancelledAt")),
                    ShippingName = reader.GetString(reader.GetOrdinal("ShippingName")),
                    ShippingAddress1 = reader.GetString(reader.GetOrdinal("ShippingAddress1")),
                    ShippingAddress2 = reader.GetString(reader.GetOrdinal("ShippingAddress2")),
                    ShippingCity = reader.GetString(reader.GetOrdinal("ShippingCity")),
                    ShippingState = reader.GetString(reader.GetOrdinal("ShippingState")),
                    ShippingPostalCode = reader.GetString(reader.GetOrdinal("ShippingPostalCode")),
                    ShippingCountry = reader.GetString(reader.GetOrdinal("ShippingCountry")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                    isPaid = reader.GetBoolean(reader.GetOrdinal("IsPaid")),
                    sessionId = reader.GetString(reader.GetOrdinal("sessionId"))
                });
            }

            await reader.CloseAsync();
            await connection.CloseAsync();

            return orders;
        }

        public async Task<bool> UpdateOrderPaymentStatusAsync(int orderId, PaymentStatus paymentStatus)
        {
            string query = @"Update Orders
set isPaid = case when @paymentStatus = @completedStatus then 1 else 0 end
where Id = @orderId;";

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@paymentStatus", paymentStatus);
            command.Parameters.AddWithValue("@completedStatus", PaymentStatus.Completed);

            int rowEffected = await command.ExecuteNonQueryAsync();

            return rowEffected > 0;
        }

        public async Task<Order?> GetOrderByCartIdAsync(int cartId)
        {
            string query = @"select o.*, oi.Quantitiy as OI_Quantity, oi.UnitPrice as OI_UnitPrice, 
                             oi.TotalPrice as OI_TotalPrice, oi.DiscountAmount as OI_DiscountAmount,
                             p.Id as P_Id, p.ProductName as P_ProductName, p.Description as P_Description,
                             p.StockQuantity as P_StockQuantity, p.ImageUrl as P_ImageUrl,
                             p.Price as P_Price, p.ReservedStock as P_ReservedStock, 
                             p.LowStockThreshold as P_LowStockThreshold, p.IsActive as P_IsActive, p.SKU as P_SKU 
                             from Orders o
                            Left join OrderItems oi on (oi.OrderId = o.Id)
                            leff join Products p on (oi.productId = p.Id)
                            where o.CartId = @cartId
                            limit 1;";

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@cartId", cartId);

            Order order = null;

            var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                if (order == null)
                {
                    order = new Order
                    {
                        Id = reader.GetInt16(reader.GetOrdinal("Id")),
                        UserId = reader.GetInt16(reader.GetOrdinal("UserId")),
                        CartId = reader.GetInt16(reader.GetOrdinal("CartId")),
                        Status = (OrderStatus)reader.GetValue(reader.GetOrdinal("Status")),
                        SubTotal = reader.GetDecimal(reader.GetOrdinal("SubTotal")),
                        TaxAmount = reader.GetDecimal(reader.GetOrdinal("TaxAmount")),
                        ShippingAmount = reader.GetDecimal(reader.GetOrdinal("ShippingAmount")),
                        Total = reader.GetDecimal(reader.GetOrdinal("Total")),
                        DiscountTotal = reader.GetDecimal(reader.GetOrdinal("DiscountTotal")),
                        CustomerEmail = reader.GetString(reader.GetOrdinal("CustomerEmail")),
                        CustomerPhone = reader.GetString(reader.GetOrdinal("CustomerPhone")),
                        ReferenceNumber = reader.GetString(reader.GetOrdinal("ReferenceNumber")),
                        CompletedAt = reader.GetDateTime(reader.GetOrdinal("CompletedAt")),
                        CancalledAt = reader.GetDateTime(reader.GetOrdinal("CancelledAt")),
                        ShippingName = reader.GetString(reader.GetOrdinal("ShippingName")),
                        ShippingAddress1 = reader.GetString(reader.GetOrdinal("ShippingAddress1")),
                        ShippingAddress2 = reader.GetString(reader.GetOrdinal("ShippingAddress2")),
                        ShippingCity = reader.GetString(reader.GetOrdinal("ShippingCity")),
                        ShippingCountry = reader.GetString(reader.GetOrdinal("ShippingCountry")),
                        ShippingPostalCode = reader.GetString(reader.GetOrdinal("ShippingPostalCode")),
                        ShippingState = reader.GetString(reader.GetOrdinal("ShippingState")),
                        isPaid = reader.GetBoolean(reader.GetOrdinal("IsPaid")),
                        sessionId = reader.GetString(reader.GetOrdinal("SessionId")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),

                        OrderItems = new List<OrderItem>()
                    };

                    var orderItem = new OrderItem
                    {
                        Id = reader.GetInt16(reader.GetOrdinal("OI_Id")),
                        ProductId = reader.GetInt16(reader.GetOrdinal("OI_ProductId")),
                        Quantity = reader.GetInt32(reader.GetOrdinal("OI_Quantity")),
                        UnitPrice = reader.GetDecimal(reader.GetOrdinal("OI_UnitPrice")),
                        TotalPrice = reader.GetDecimal(reader.GetOrdinal("OI_TotalPrice")),
                        DiscountAmount = reader.GetDecimal(reader.GetOrdinal("OI_DiscountAmount")),

                        Product = new Product
                        {
                            Id = reader.GetInt16(reader.GetOrdinal("P_Id")),
                            Name = reader.GetString(reader.GetOrdinal("P_ProductName")),
                            Description = reader.GetString(reader.GetOrdinal("P_Description")),
                            StockQuantity = reader.GetInt32(reader.GetOrdinal("P_StockQuantity")),
                            ImageUrl = reader.GetString(reader.GetOrdinal("P_ImageUrl")),
                            Price = reader.GetDecimal(reader.GetOrdinal("P_Price")),
                            ReservedStock = reader.GetInt32(reader.GetOrdinal("P_ReservedStock")),
                            LowStockThreshold = reader.GetInt32(reader.GetOrdinal("P_LowStockThreshold")),
                            IsActive = reader.GetBoolean(reader.GetOrdinal("P_IsActive")),
                            SKU = reader.GetString(reader.GetOrdinal("P_SKU"))
                        }
                    };

                    order.OrderItems.Add(orderItem);
                }
            }

            return order;
        }

        public async Task<bool> UpdateOrderAsync(Order order)
        {
            string query = @"Update Orders
set UserId = @userId,
CartId = @cartId,
Status = @status,
SubTotal = @subTotal,
TaxAmount = @taxAmount,
ShippingAmount = @shippingAmount,
Total = @total,
DiscountTotal = @discountTotal,
CustomerEmail = @customerEmail,
CustomerPhone = @customerPhone,
ReferenceNumber = @referenceNumber,
CompletedAt = @completedAt,
CancelledAt = @cancelledAt,
ShippingName, = @shippingName,
ShippingAddress1 = @shippingAddress1,
ShippingAddress2 = @shippingAddress2,
ShippingCity = @shippingCity,
ShippingState= @shippingState,
ShippingPostalCode, @shippingPostalCode,
ShippingCountry, @shippingCountry,
CreatedAt = @createdAt,
UpdatedAt = @updatedAt,
IsPaid = @isPaid,
SessionId= @sessionId
where Id = @orderId;";

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@userId", order.UserId);
            command.Parameters.AddWithValue("@cartId", order.CartId);
            command.Parameters.AddWithValue("@status", order.Status);
            command.Parameters.AddWithValue("@subTotal", order.SubTotal);
            command.Parameters.AddWithValue("@taxAmount", order.TaxAmount);
            command.Parameters.AddWithValue("@shippingAmount", order.ShippingAmount);
            command.Parameters.AddWithValue("@total", order.Total);
            command.Parameters.AddWithValue("@discountTotal", order.DiscountTotal);
            command.Parameters.AddWithValue("@customerEmail", order.CustomerEmail);
            command.Parameters.AddWithValue("@customerPhone", order.CustomerPhone);
            command.Parameters.AddWithValue("@referenceNumber", order.ReferenceNumber);
            command.Parameters.AddWithValue("@completedAt", order.CompletedAt);
            command.Parameters.AddWithValue("@cancelledAt", order.CancalledAt);
            command.Parameters.AddWithValue("@shippingName", order.ShippingName);
            command.Parameters.AddWithValue("@shippingAddress1", order.ShippingAddress1);
            command.Parameters.AddWithValue("@shippingAddress2", order.ShippingAddress2);
            command.Parameters.AddWithValue("@shippingCity", order.ShippingCity);
            command.Parameters.AddWithValue("@shippingState", order.ShippingState);
            command.Parameters.AddWithValue("@shippingPostalCode", order.ShippingPostalCode);
            command.Parameters.AddWithValue("@shippingCountry", order.ShippingCountry);
            command.Parameters.AddWithValue("@createdAt", order.CreatedAt);
            command.Parameters.AddWithValue("@updatedAt", order.UpdatedAt);
            command.Parameters.AddWithValue("@isPaid", order.isPaid);
            command.Parameters.AddWithValue("@sessionId", order.sessionId);
            command.Parameters.AddWithValue("@orderId", order.Id);


            int rowEffected = await command.ExecuteNonQueryAsync();

            return rowEffected > 0;
        }

        public async Task<bool> DeleteOrderItemsAsync(int orderId)
        {
            string query = @"Delete from order_items oi where oi.orderId = @orderId;";

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

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

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@orderId", userId);

            object result = await command.ExecuteScalarAsync();

            await connection.CloseAsync();

            return Convert.ToBoolean(result);
        }
    }
}
