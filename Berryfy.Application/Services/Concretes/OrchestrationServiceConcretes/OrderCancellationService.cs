using Berryfy.Application.Services.Interfaces.CouponServiceInterfaces;
using Berryfy.Application.Services.Interfaces.InventoryServiceInterfaces;
using Berryfy.Application.Services.Interfaces.OrchestrationServiceInterfaces;
using Berryfy.Domain.Constants;
using Berryfy.Domain.Repositories;
using Berryfy.Domain.Repositories.OrderInterfaces;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Berryfy.Application.Services.Concretes.OrchestrationServiceConcretes
{
    public class OrderCancellationService : IOrderCancellationService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IInventoryService _inventoryService;
        private readonly IUserCouponService _userCouponService;
        private readonly ILogger<OrderCancellationService> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public OrderCancellationService(
            IUnitOfWork unitOfWork,
            IOrderRepository orderRepository,
            IInventoryService inventoryService,
            IUserCouponService userCouponService,
            ILogger<OrderCancellationService> logger)
        {
            _orderRepository = orderRepository;
            _inventoryService = inventoryService;
            _userCouponService = userCouponService;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task<CancellationResult> CancelOrderAsync(int orderId, string reason, int? performedByUserId = null)
        {
            var result = new CancellationResult();

            try
            {
                _logger.LogInformation("Starting order cancellation for order {OrderId}", orderId);

                var order = await _orderRepository.GetOrderByIdAsync(orderId);
                if (order == null)
                {
                    result.ErrorMessage = "Order not found";
                    return result;
                }

                if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Processing)
                {
                    result.ErrorMessage = $"Cannot cancel order with status {order.Status}. Only Pending or Processing orders can be cancelled.";
                    return result;
                }

                // Start the raw ADO.NET transaction
                await _unitOfWork.BeginTransactionAsync();

                try
                {
                    _logger.LogDebug("Reverting inventory for cancelled order {OrderId} (status {Status})", orderId, order.Status);

                    if (order.Status == OrderStatus.Pending)
                    {
                        if (order.CartId <= 0)
                        {
                            result.Warnings.Add("Order has no cart id; reserved stock may not have been released");
                        }
                        else
                        {
                            foreach (var item in order.OrderItems)
                            {
                                var released = await _inventoryService.ReleaseReservedStockAsync(
                                    item.ProductId,
                                    item.Quantity,
                                    order.CartId,
                                    "CartItem");

                                if (!released)
                                {
                                    result.Warnings.Add($"Failed to release reserved stock for product {item.ProductId}");
                                }
                                else
                                {
                                    result.InventoryRestored = true;
                                }
                            }
                        }
                    }
                    else if (order.Status == OrderStatus.Processing)
                    {
                        foreach (var item in order.OrderItems)
                        {
                            var inventoryRestored = await _inventoryService.AddStockAsync(
                                item.ProductId,
                                item.Quantity,
                                $"Stock returned from cancelled order {order.ReferenceNumber}: {reason}",
                                performedByUserId);

                            if (!inventoryRestored)
                            {
                                result.Warnings.Add($"Failed to restore inventory for product {item.ProductId}");
                            }
                            else
                            {
                                result.InventoryRestored = true;
                            }
                        }
                    }

                    _logger.LogDebug("Reverting coupon usage for cancelled order {OrderId}", orderId);
                    if (order.UserId > 0)
                    {
                        var couponIds = await _userCouponService.GetCouponIdsUsedInOrderAsync(orderId);
                        foreach (var couponId in couponIds)
                        {
                            var couponReverted = await _userCouponService.RevertCouponUsageAsync(
                                order.UserId,
                                couponId,
                                orderId);

                            if (!couponReverted)
                            {
                                result.Warnings.Add($"Failed to revert coupon {couponId} usage");
                            }
                            else
                            {
                                result.CouponsReverted = true;
                            }
                        }
                    }

                    _logger.LogDebug("Updating order status to Cancelled for order {OrderId}", orderId);
                    order.Status = OrderStatus.Cancelled;
                    order.CancalledAt = DateTime.UtcNow;
                    order.UpdatedAt = DateTime.UtcNow;

                    var orderUpdated = await _orderRepository.UpdateOrderStatusAsync(order.Id, order.Status);
                    if (!orderUpdated)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        result.ErrorMessage = "Failed to update order status to Cancelled";
                        return result; // Replaced 'return false'
                    }

                    var committed = await _unitOfWork.CommitTransactionAsync();
                    if (!committed)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        result.ErrorMessage = "Failed to commit cancellation transaction";
                        return result; // Replaced 'return false'
                    }

                    result.IsSuccess = true;
                    _logger.LogInformation("Successfully cancelled order {OrderId}", orderId);

                    return result; // Replaced 'return true'
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during order cancellation transaction for order {OrderId}", orderId);
                    await _unitOfWork.RollbackTransactionAsync();
                    result.ErrorMessage = $"Cancellation transaction failed: {ex.Message}";

                    return result; // Replaced 'return false'
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during order cancellation for order {OrderId}", orderId);
                result.ErrorMessage = $"Unexpected error: {ex.Message}";
                return result;
            }
        }
    }
}
