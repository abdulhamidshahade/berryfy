using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.OrderDtos.Requests;
using Berryfy.Application.Dtos.OrderDtos.Responses;
using Berryfy.Domain.Constants;
using Berryfy.Domain.Entities.OrderEntities;
namespace Berryfy.Application.Services.Interfaces.OrderServiceInterfaces
{
    public interface IOrderService
    {
        Task<ApplicationResponse<OrderResponse?>> GetOrderByIdAsync(int orderId);

        Task<ApplicationResponse<List<OrderResponse>>> GetUserOrdersAsync(int userId, int page = 1, int pageSize = 10);
        Task<ApplicationResponse<List<OrderResponse>>> GetAllOrdersAsync(int page = 1, int pageSize = 50);
        Task<ApplicationResponse<Order?>> CreateOrderFromCartAsync(int cartId, CreateOrder orderDto);
        Task<ApplicationResponse<bool>> UpdateOrderStatusAsync(Order order, OrderStatus newStatus);
        Task<ApplicationResponse<bool>> UpdateOrderPaymentStatusAsync(Order order, PaymentStatus paymentStatus);
        Task<ApplicationResponse<bool>> CancelOrderAsync(int orderId, string reason);
        Task<ApplicationResponse<bool>> RefundOrderAsync(int orderId, string reason);
        Task<ApplicationResponse<bool>> ProcessOrderAsync(int orderId);
        Task<ApplicationResponse<OrderTotal>> CalculateOrderTotalsAsync(int cartId);
        Task<ApplicationResponse<string>> GenerateUniqueReferenceNumberAsync();
        Task<ApplicationResponse<List<Order>>> GetOrdersByStatusAsync(OrderStatus status, int page = 1, int pageSize = 10);
        Task<ApplicationResponse<Order?>> GetOrderByReferenceNumberAsync(string referenceNumber);
        Task<ApplicationResponse<Order?>> GetOrderByCartIdAsync(int cartId);
        Task<ApplicationResponse<bool>> SyncOrderWithCartAsync(int orderId, int cartId);
        Task<ApplicationResponse<bool>> DeductInventoryForPaidOrderAsync(int orderId);
    }
}
