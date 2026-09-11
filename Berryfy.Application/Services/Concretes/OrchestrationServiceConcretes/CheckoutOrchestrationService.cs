using Berryfy.Application.Dtos.OrderDtos;
using Berryfy.Application.Services.Interfaces.CouponServiceInterfaces;
using Berryfy.Application.Services.Interfaces.InventoryServiceInterfaces;
using Berryfy.Application.Services.Interfaces.OrderServiceInterfaces;
using Berryfy.Application.Services.Interfaces.OrchestrationServiceInterfaces;
using Berryfy.Application.Services.Interfaces.ShoppingCartServiceInterfaces;
using Berryfy.Domain.Constants;
using Berryfy.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Berryfy.Application.Services.Concretes.OrchestrationServiceConcretes
{
    public class CheckoutOrchestrationService : ICheckoutOrchestrationService
    {
        private readonly ICartService _cartService;
        private readonly IOrderService _orderService;
        private readonly IInventoryService _inventoryService;
        private readonly IUserCouponService _userCouponService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CheckoutOrchestrationService> _logger;

        public CheckoutOrchestrationService(
            ICartService cartService,
            IOrderService orderService,
            IInventoryService inventoryService,
            IUserCouponService userCouponService,
            IUnitOfWork unitOfWork,
            ILogger<CheckoutOrchestrationService> logger)
        {
            _cartService = cartService;
            _orderService = orderService;
            _inventoryService = inventoryService;
            _userCouponService = userCouponService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<CheckoutResult> ProcessCheckoutAsync(int cartId, CreateOrderDto orderDto, int? userId)
        {
            var result = new CheckoutResult();

            try
            {
                _logger.LogInformation("Starting checkout process for cart {CartId}", cartId);

                // Try Active status first, then PendingPayment
                var cart = await _cartService.GetCartByIdAsync(cartId, CartStatus.Active);
                if (cart == null)
                {
                    cart = await _cartService.GetCartByIdAsync(cartId, CartStatus.PendingPayment);
                }
                
                if (cart == null)
                {
                    result.ErrorMessage = "Cart not found or already completed";
                    return result;
                }

                if (cart.CartItems == null || cart.CartItems.Count == 0)
                {
                    result.ErrorMessage = "Cart is empty";
                    return result;
                }

                if (!userId.HasValue || cart.UserId != userId || orderDto.UserId != userId)
                {
                    result.ErrorMessage = "You can only check out your own cart";
                    return result;
                }

                // If cart is already PendingPayment, check if order exists
                if (cart.Status == CartStatus.PendingPayment)
                {
                    _logger.LogInformation("Cart {CartId} is already in PendingPayment status, checking for existing order", cartId);
                    var existingOrder = await _orderService.GetOrderByCartIdAsync(cartId);
                    if (existingOrder != null)
                    {
                        _logger.LogInformation("Found existing order {OrderId} for cart {CartId}, syncing and returning it", existingOrder.Id, cartId);
                        // Sync the order with current cart state (in case items were modified)
                        if (!await _orderService.SyncOrderWithCartAsync(existingOrder.Id, cartId))
                        {
                            result.ErrorMessage = "Could not update the pending order";
                            return result;
                        }
                        result.Order = await _orderService.GetOrderByCartIdAsync(cartId);
                        result.IsSuccess = true;
                        return result;
                    }
                    _logger.LogInformation("No existing order found for PendingPayment cart {CartId}, will create new order", cartId);
                }

                foreach (var item in cart.CartItems)
                {
                    // Cart additions already reserved these units. Available stock excludes them.
                    var product = await _inventoryService.GetProductWithStockInfoAsync(item.ProductId);
                    if (item.Quantity <= 0 || product == null || product.ReservedStock < item.Quantity ||
                        product.StockQuantity < product.ReservedStock)
                    {
                        result.ErrorMessage = $"Insufficient stock for product ID {item.ProductId}";
                        return result;
                    }
                }

                var strategy = _unitOfWork.BeginTransactionAsyncStrategy();
                var transactionResult = await strategy.ExecuteAsync(async () =>
                {
                    await _unitOfWork.BeginTransactionAsync();

                    try
                    {
                        _logger.LogDebug("Creating order from cart {CartId}", cartId);
                        var order = await _orderService.CreateOrderFromCartAsync(cartId, orderDto);
                        if (order == null)
                        {
                            await _unitOfWork.RollbackTransactionAsync();
                            result.ErrorMessage = "Failed to create order";
                            return false;
                        }

                        result.Order = order;

                        var committed = await _unitOfWork.CommitTransactionAsync();
                        if (!committed)
                        {
                            await _unitOfWork.RollbackTransactionAsync();
                            result.ErrorMessage = "Failed to commit checkout transaction";
                            return false;
                        }

                        result.IsSuccess = true;
                        _logger.LogInformation("Successfully completed checkout for cart {CartId}, order {OrderId}",
                            cartId, order.Id);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during checkout transaction for cart {CartId}", cartId);
                        await _unitOfWork.RollbackTransactionAsync();
                        result.ErrorMessage = $"Checkout transaction failed: {ex.Message}";
                        return false;
                    }
                });

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during checkout for cart {CartId}", cartId);
                result.ErrorMessage = $"Unexpected error: {ex.Message}";
                return result;
            }
        }
    }
}
