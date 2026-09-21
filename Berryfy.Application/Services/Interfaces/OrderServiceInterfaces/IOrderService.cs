using Berryfy.Application.Dtos.OrderDtos.Requests;
using Berryfy.Application.Dtos.OrderDtos.Responses;
using Berryfy.Domain.Constants;
using Berryfy.Domain.Entities.OrderEntities;
namespace Berryfy.Application.Services.Interfaces.OrderServiceInterfaces
{
    public interface IOrderService
    {
        Task<OrderResponse?> GetOrderByIdAsync(int orderId);

        Task<List<OrderResponse>> GetUserOrdersAsync(int userId, int page = 1, int pageSize = 10);
        Task<List<OrderResponse>> GetAllOrdersAsync(int page = 1, int pageSize = 50);
        Task<Order?> CreateOrderFromCartAsync(int cartId, CreateOrder orderDto);
        Task<bool> UpdateOrderStatusAsync(Order order, OrderStatus newStatus);
        Task<bool> UpdateOrderPaymentStatusAsync(Order order, PaymentStatus paymentStatus);
        Task<bool> CancelOrderAsync(int orderId, string reason);
        Task<bool> RefundOrderAsync(int orderId, string reason);
        Task<bool> ProcessOrderAsync(int orderId);
        Task<OrderTotal> CalculateOrderTotalsAsync(int cartId);
        Task<string> GenerateUniqueReferenceNumberAsync();
        Task<List<Order>> GetOrdersByStatusAsync(OrderStatus status, int page = 1, int pageSize = 10);
        Task<Order?> GetOrderByReferenceNumberAsync(string referenceNumber);
        Task<Order?> GetOrderByCartIdAsync(int cartId);
        Task<bool> SyncOrderWithCartAsync(int orderId, int cartId);
        Task<bool> DeductInventoryForPaidOrderAsync(int orderId);
    }
}
