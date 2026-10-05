using Berryfy.Domain.Constants;
using Berryfy.Domain.Entities;
using Berryfy.Domain.Entities.OrderEntities;

namespace Berryfy.Domain.Repositories.OrderInterfaces
{
    public interface IOrderRepository
    {
        Task<InfrastructureResponse<Order?>> GetOrderByIdAsync(int orderId);
        Task<InfrastructureResponse<List<Order>>> GetUserOrdersAsync(int userId, int page = 1, int pageSize = 10);
        Task<InfrastructureResponse<List<Order>>> GetAllOrdersAsync(int page = 1, int pageSize = 50);
        Task<InfrastructureResponse<bool>> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus);
        Task<InfrastructureResponse<Order>> CreateOrderAsync(Order order);
        Task<InfrastructureResponse<OrderItem>> CreateOrderItemAsync(OrderItem item);
        Task<InfrastructureResponse<Order?>> GetOrderByReferenceNumberAsync(string referenceNumber);
        Task<InfrastructureResponse<List<Order>>> GetOrdersByStatusAsync(OrderStatus status, int page = 1, int pageSize = 10);
        Task<InfrastructureResponse<bool>> UpdateOrderPaymentStatusAsync(int orderId, PaymentStatus paymentStatus);
        Task<InfrastructureResponse<Order?>> GetOrderByCartIdAsync(int cartId);
        Task<InfrastructureResponse<bool>> UpdateOrderAsync(Order order);
        Task<InfrastructureResponse<bool>> DeleteOrderItemsAsync(int orderId);
        Task<InfrastructureResponse<bool>> UserHasPaidOrderAsync(int userId);
    }
}
