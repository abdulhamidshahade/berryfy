using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.OrderDtos;
using Berryfy.Application.Dtos.OrderDtos.Requests;
using Berryfy.Application.Services.Interfaces.CouponServiceInterfaces;
using Berryfy.Application.Services.Interfaces.InventoryServiceInterfaces;
using Berryfy.Application.Services.Interfaces.OrchestrationServiceInterfaces;
using Berryfy.Application.Services.Interfaces.OrderServiceInterfaces;
using Berryfy.Application.Services.Interfaces.ShoppingCartServiceInterfaces;
using Berryfy.Domain.Constants;
using Berryfy.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Berryfy.Application.Services.Concretes.OrchestrationServiceConcretes
{
    public class CheckoutOrchestrationService : ICheckoutOrchestrationService
    {
        private readonly ICartService _cartService;
        private readonly IOrderService _orderService;
        private readonly IInventoryService _inventoryService;
        private readonly IUserCouponService _userCouponService;
        private readonly ILogger<CheckoutOrchestrationService> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public CheckoutOrchestrationService(
            IUnitOfWork unitOfWork,
            ICartService cartService,
            IOrderService orderService,
            IInventoryService inventoryService,
            IUserCouponService userCouponService,
            ILogger<CheckoutOrchestrationService> logger)
        {
            _cartService = cartService;
            _orderService = orderService;
            _inventoryService = inventoryService;
            _userCouponService = userCouponService;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task<ApplicationResponse<CheckoutResult>> ProcessCheckoutAsync(int cartId, CreateOrder orderDto, int? userId)
        {
            var result = new CheckoutResult();

            try
            {
                _logger.LogInformation("Starting checkout process for cart {CartId}", cartId);

                // Try Active status first, then PendingPayment
                var cart = _cartService.GetCartByIdAsync(cartId, CartStatus.Active).GetAwaiter().GetResult().Value;
                if (cart == null)
                {
                    cart = _cartService.GetCartByIdAsync(cartId, CartStatus.PendingPayment).GetAwaiter().GetResult().Value;
                }

                if (cart == null)
                {
                    result.ErrorMessage = "Cart not found or already completed";
                    return new ApplicationResponse<CheckoutResult>
                    {
                        Value = result,
                        IsSuccess = false,
                        ErrorMessage = "Cart not found or already completed"
                    };
                }

                if (cart.CartItems == null || cart.CartItems.Count == 0)
                {
                    result.ErrorMessage = "Cart is empty";
                    return new ApplicationResponse<CheckoutResult>
                    {
                        Value = result,
                        IsSuccess = false,
                        ErrorMessage = "Cart is empty"
                    };
                }

                if (!userId.HasValue || cart.UserId != userId || orderDto.UserId != userId)
                {
                    result.ErrorMessage = "You can only check out your own cart";
                    return new ApplicationResponse<CheckoutResult>
                    {
                        Value = result,
                        IsSuccess = false,
                        ErrorMessage = "You can only check out your own cart"
                    };
                }

                // If cart is already PendingPayment, check if order exists
                if (cart.Status == CartStatus.PendingPayment)
                {
                    _logger.LogInformation("Cart {CartId} is already in PendingPayment status, checking for existing order", cartId);
                    var existingOrder = _orderService.GetOrderByCartIdAsync(cartId).GetAwaiter().GetResult().Value;
                    if (existingOrder != null)
                    {
                        _logger.LogInformation("Found existing order {OrderId} for cart {CartId}, syncing and returning it", existingOrder.Id, cartId);
                        if (!_orderService.SyncOrderWithCartAsync(existingOrder.Id, cartId).GetAwaiter().GetResult().Value)
                        {
                            result.ErrorMessage = "Could not update the pending order";
                            return new ApplicationResponse<CheckoutResult>
                            {
                                Value = result,
                                IsSuccess = false,
                                ErrorMessage = "Could not update the pending order"
                            };
                        }
                        result.Order = _orderService.GetOrderByCartIdAsync(cartId).GetAwaiter().GetResult().Value;
                        result.IsSuccess = true;
                        return new ApplicationResponse<CheckoutResult>
                        {
                            Value = result,
                            IsSuccess = true
                        };
                    }
                    _logger.LogInformation("No existing order found for PendingPayment cart {CartId}, will create new order", cartId);
                }

                // IMPORTANT: Start the transaction BEFORE checking inventory.
                // This allows your underlying InventoryRepository to use a "SELECT ... FOR UPDATE" lock
                // so two users can't buy the last item at the exact same millisecond.
                await _unitOfWork.BeginTransactionAsync();

                try
                {
                    foreach (var item in cart.CartItems)
                    {
                        var product = _inventoryService.GetProductWithStockInfoAsync(item.ProductId).GetAwaiter().GetResult().Value;
                        if (item.Quantity <= 0 || product == null || product.ReservedStock < item.Quantity ||
                            product.StockQuantity < product.ReservedStock)
                        {
                            await _unitOfWork.RollbackTransactionAsync();
                            result.ErrorMessage = $"Insufficient stock for product ID {item.ProductId}";
                            return new ApplicationResponse<CheckoutResult>
                            {
                                Value = result,
                                IsSuccess = false,
                                ErrorMessage = $"Insufficient stock for product ID {item.ProductId}"
                            };
                        }
                    }

                    _logger.LogDebug("Creating order from cart {CartId}", cartId);
                    var order = _orderService.CreateOrderFromCartAsync(cartId, orderDto).GetAwaiter().GetResult().Value;

                    if (order == null)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        result.ErrorMessage = "Failed to create order";
                        return new ApplicationResponse<CheckoutResult>
                        {
                            Value = result,
                            IsSuccess = false,
                            ErrorMessage = "Failed to create order"
                        };
                    }

                    result.Order = order;

                    var committed = await _unitOfWork.CommitTransactionAsync();
                    if (!committed)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        result.ErrorMessage = "Failed to commit checkout transaction";
                        return new ApplicationResponse<CheckoutResult>
                        {
                            Value = result,
                            IsSuccess = false,
                            ErrorMessage = "Failed to commit checkout transaction"
                        };
                    }

                    result.IsSuccess = true;
                    _logger.LogInformation("Successfully completed checkout for cart {CartId}, order {OrderId}", cartId, order.Id);

                    return new ApplicationResponse<CheckoutResult>
                    {
                        Value = result,
                        IsSuccess = true
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during checkout transaction for cart {CartId}", cartId);
                    await _unitOfWork.RollbackTransactionAsync();
                    result.ErrorMessage = $"Checkout transaction failed: {ex.Message}";
                    return new ApplicationResponse<CheckoutResult>
                    {
                        Value = result,
                        IsSuccess = false,
                        ErrorMessage = $"Checkout transaction failed: {ex.Message}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during checkout for cart {CartId}", cartId);
                result.ErrorMessage = $"Unexpected error: {ex.Message}";
                return new ApplicationResponse<CheckoutResult>
                {
                    Value = result,
                    IsSuccess = false,
                    ErrorMessage = $"Unexpected error: {ex.Message}"
                };
            }
        }
    }
}