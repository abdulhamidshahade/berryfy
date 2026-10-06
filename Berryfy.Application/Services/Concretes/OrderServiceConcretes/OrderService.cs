using AutoMapper;
using Berryfy.Application.Services.Interfaces.InventoryServiceInterfaces;
using Berryfy.Application.Services.Interfaces.OrderServiceInterfaces;
using Berryfy.Application.Services.Interfaces.OrchestrationServiceInterfaces;
using Berryfy.Application.Services.Interfaces.ShoppingCartServiceInterfaces;
using Berryfy.Domain.Constants;
using Berryfy.Domain.Entities.OrderEntities;
using Berryfy.Domain.Repositories.OrderInterfaces;
using Microsoft.Extensions.Logging;
using Berryfy.Application.Dtos.OrderDtos.Requests;
using Berryfy.Application.Dtos.OrderDtos.Responses;
using Berryfy.Application.Dtos;

namespace Berryfy.Application.Services.Concretes.OrderServiceConcretes
{
    public class OrderService : IOrderService
    {
        private readonly ICartService _cartService;
        private readonly IOrderRepository _orderRepository;
        private readonly IInventoryService _inventoryService;
        private readonly IOrderCancellationService _orderCancellationService;
        private readonly IRefundOrchestrationService _refundOrchestrationService;
        private readonly ILogger<OrderService> _logger;

        public OrderService(ICartService cartService,
                            IOrderRepository orderRepository,
                            IInventoryService inventoryService,
                            IOrderCancellationService orderCancellationService,
                            IRefundOrchestrationService refundOrchestrationService,
                            ILogger<OrderService> logger)
        {
            _cartService = cartService;
            _orderRepository = orderRepository;
            _inventoryService = inventoryService;
            _orderCancellationService = orderCancellationService;
            _refundOrchestrationService = refundOrchestrationService;
            _logger = logger;
        }


        public async Task<ApplicationResponse<OrderTotal>> CalculateOrderTotalsAsync(int cartId)
        {
            var cart = await _cartService.GetCartByIdAsync(cartId, CartStatus.Active)
                ?? await _cartService.GetCartByIdAsync(cartId, CartStatus.PendingPayment);

            if (cart == null)
            {
                return new ApplicationResponse<OrderTotal> { IsSuccess = false, Value = null };
            }

            decimal subTotal = cart.CartItems.Sum(item => item.UnitPrice * item.Quantity);
            decimal discountTotal = Math.Clamp(cart.CartCoupons?.Sum(coupon => coupon.DiscountAmount) ?? 0, 0, subTotal);
            decimal taxAmount = PricingPolicy.Tax(subTotal, discountTotal);
            decimal shippingAmount = PricingPolicy.Shipping(subTotal);

            return new ApplicationResponse<OrderTotal>
            {
                IsSuccess = true,
                Value = new OrderTotal
                {
                    SubTotal = subTotal,
                    DiscountTotal = discountTotal,
                    TaxAmount = taxAmount,
                    ShippingAmount = shippingAmount,
                    Total = subTotal - discountTotal + taxAmount + shippingAmount
                }
            };
        }

        public async Task<ApplicationResponse<bool>> CancelOrderAsync(int orderId, string reason)
        {
            var result = await _orderCancellationService.CancelOrderAsync(orderId, reason);
            return new ApplicationResponse<bool> { IsSuccess = result.IsSuccess, Value = result.IsSuccess };
        }

        public async Task<ApplicationResponse<Order?>> CreateOrderFromCartAsync(int cartId, CreateOrder orderDto)
        {
            var cart = _cartService.GetCartByIdAsync(cartId, CartStatus.Active).GetAwaiter().GetResult().Value;
            if (cart == null)
            {
                cart = await _cartService.GetCartByIdAsync(cartId, CartStatus.PendingPayment);
            }

            if (cart == null || orderDto.UserId <= 0 || cart.UserId != orderDto.UserId)
            {
                return new ApplicationResponse<Order?> { IsSuccess = false, Value = null };
            }

            var existingOrder = await _orderRepository.GetOrderByCartIdAsync(cartId);
            if (existingOrder != null)
            {
                return new ApplicationResponse<Order?> { IsSuccess = false, Value = null };
            }

            if (cart.CartItems == null || !cart.CartItems.Any())
            {
                return new ApplicationResponse<Order?> { IsSuccess = false, Value = null };
            }

            decimal subTotal = cart.CartItems.Sum(item => item.UnitPrice * item.Quantity);
            decimal discountTotal = Math.Clamp(cart.CartCoupons?.Sum(coupon => coupon.DiscountAmount) ?? 0, 0, subTotal);
            decimal taxAmount = PricingPolicy.Tax(subTotal, discountTotal);
            decimal shippingAmount = PricingPolicy.Shipping(subTotal);
            decimal total = subTotal - discountTotal + taxAmount + shippingAmount;

            var referenceNumber = await GenerateUniqueReferenceNumberAsync();

            var order = new Order
            {
                UserId = orderDto.UserId,
                CartId = cartId,
                ReferenceNumber = referenceNumber,
                Status = OrderStatus.Pending,
                SubTotal = subTotal,
                DiscountTotal = discountTotal,
                TaxAmount = taxAmount,
                ShippingAmount = shippingAmount,
                Total = total,
                CustomerEmail = orderDto.CustomerEmail,
                CustomerPhone = orderDto.CustomerPhone,
                ShippingName = orderDto.ShippingName,
                ShippingAddress1 = orderDto.ShippingAddressLine1,
                ShippingAddress2 = orderDto.ShippingAddressLine2,
                ShippingCity = orderDto.ShippingCity,
                ShippingState = orderDto.ShippingState,
                ShippingPostalCode = orderDto.ShippingPostalCode,
                ShippingCountry = orderDto.ShippingCountry,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            order = _orderRepository.CreateOrderAsync(order).GetAwaiter().GetResult().Value;

            if (order == null)
            {
                return new ApplicationResponse<Order?> { IsSuccess = false, Value = null };
            }

            foreach (var cartItem in cart.CartItems)
            {
                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = cartItem.ProductId,
                    ProductName = cartItem.Product?.Name ?? "Unknown Product",
                    Quantity = cartItem.Quantity,
                    UnitPrice = cartItem.UnitPrice,
                    TotalPrice = cartItem.UnitPrice * cartItem.Quantity,
                    DiscountAmount = 0
                };

                await _orderRepository.CreateOrderItemAsync(orderItem);
            }

            var cartStatusUpdated = await _cartService.UpdateCartStatusAsync(cartId, CartStatus.PendingPayment);
            if (!cartStatusUpdated)
            {
                throw new InvalidOperationException($"Failed to update cart {cartId} status to PendingPayment. Order creation aborted.");
            }

            return new ApplicationResponse<Order?> { IsSuccess = true, Value = order };
        }

        public async Task<ApplicationResponse<bool>> RefundOrderAsync(int orderId, string reason)
        {
            var result = await _refundOrchestrationService.ProcessRefundAsync(orderId, reason);
            return new ApplicationResponse<bool> { IsSuccess = result.IsSuccess, Value = result.IsSuccess };
        }

        public async Task<ApplicationResponse<bool>> UpdateOrderStatusAsync(Order order, OrderStatus newStatus)
        {
            if (order == null || !Enum.IsDefined(newStatus)) return new ApplicationResponse<bool> { IsSuccess = false, Value = false };

            // Settlement and returns must use their dedicated workflows.
            if (newStatus is OrderStatus.Cancelled or OrderStatus.Refunded) return new ApplicationResponse<bool> { IsSuccess = false, Value = false };
            if (order.Status == newStatus) return new ApplicationResponse<bool> { IsSuccess = true, Value = true };
            if (!order.isPaid) return new ApplicationResponse<bool> { IsSuccess = false, Value = false };

            var allowed = (order.Status, newStatus) switch
            {
                (OrderStatus.Pending, OrderStatus.Processing) => true,
                (OrderStatus.Processing, OrderStatus.Shipped) => true,
                (OrderStatus.Shipped, OrderStatus.Delivered) => true,
                (OrderStatus.Delivered, OrderStatus.Completed) => true,
                _ => false
            };
            if (!allowed) return new ApplicationResponse<bool> { IsSuccess = false, Value = false };

            var saved = _orderRepository.UpdateOrderStatusAsync(order.Id, newStatus).GetAwaiter().GetResult().Value;
            if (saved)
            {
                order.Status = newStatus;
                order.UpdatedAt = DateTime.UtcNow;
                if (newStatus == OrderStatus.Completed) order.CompletedAt = DateTime.UtcNow;
            }
            return new ApplicationResponse<bool> { IsSuccess = saved, Value = saved };
        }

        public async Task<ApplicationResponse<string>> GenerateUniqueReferenceNumberAsync()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var randomComponent = new Random().Next(1000, 9999);
            var referenceNumber = $"ORD-{timestamp}-{randomComponent}";

            var existingOrder = _orderRepository.GetOrderByReferenceNumberAsync(referenceNumber).GetAwaiter().GetResult().Value;

            if (existingOrder != null)
            {
                var additionalRandom = new Random().Next(100, 999);
                referenceNumber = $"ORD-{timestamp}-{randomComponent}-{additionalRandom}";
            }

            return new ApplicationResponse<string> { IsSuccess = true, Value = referenceNumber };
        }

        public async Task<ApplicationResponse<OrderResponse?>> GetOrderByIdAsync(int orderId)
        {
            var order = _orderRepository.GetOrderByIdAsync(orderId).GetAwaiter().GetResult().Value;

            return new ApplicationResponse<OrderResponse?> { IsSuccess = true, Value = OrderResponse.MapFromOrder(order) };
        }

        public async Task<ApplicationResponse<Order?>> GetOrderByReferenceNumberAsync(string referenceNumber)
        {
            var order = _orderRepository.GetOrderByReferenceNumberAsync(referenceNumber).GetAwaiter().GetResult().Value;
            return new ApplicationResponse<Order?> { IsSuccess = true, Value = order };
        }

        public async Task<ApplicationResponse<List<Order>>> GetOrdersByStatusAsync(OrderStatus status, int page = 1, int pageSize = 10)
        {
            var orders = _orderRepository.GetOrdersByStatusAsync(status, page, pageSize).GetAwaiter().GetResult().Value;
            return new ApplicationResponse<List<Order>>() { IsSuccess = true, Value = orders };
        }

        public async Task<ApplicationResponse<List<OrderResponse>>> GetUserOrdersAsync(int userId, int page = 1, int pageSize = 10)
        {
            var orders = _orderRepository.GetUserOrdersAsync(userId, page, pageSize).GetAwaiter().GetResult().Value;

            var mappedOrder = OrderResponse.MapFromOrder(orders);
            return new ApplicationResponse<List<OrderResponse>> { IsSuccess = true, Value = mappedOrder };
        }

        public async Task<ApplicationResponse<List<OrderResponse>>> GetAllOrdersAsync(int page = 1, int pageSize = 50)
        {
            var orders = _orderRepository.GetAllOrdersAsync(page, pageSize).GetAwaiter().GetResult().Value;

            var mappedOrders = OrderResponse.MapFromOrder(orders);
            return new ApplicationResponse<List<OrderResponse>> { IsSuccess = true, Value = mappedOrders };
        }


        public async Task<bool> ProcessOrderAsync(int orderId)
        {
            var order = _orderRepository.GetOrderByIdAsync(orderId).GetAwaiter().GetResult().Value;

            if (order == null)
            {
                return false;
            }

            if (order.Status != OrderStatus.Pending)
            {
                return false;
            }

            var orderStatusUpdated = UpdateOrderStatusAsync(order, OrderStatus.Processing).GetAwaiter().GetResult().Value;

            return orderStatusUpdated;
        }

        public Task<ApplicationResponse<bool>> UpdateOrderPaymentStatusAsync(Order order, PaymentStatus paymentStatus)
        {
            return _orderRepository.UpdateOrderPaymentStatusAsync(order.Id, paymentStatus).GetAwaiter().GetResult().Value
                ? Task.FromResult(new ApplicationResponse<bool> { IsSuccess = true, Value = true })
                : Task.FromResult(new ApplicationResponse<bool> { IsSuccess = true, Value = false });
        }

        public async Task<ApplicationResponse<Order>> GetOrderByCartIdAsync(int cartId)
        {
            var order = _orderRepository.GetOrderByCartIdAsync(cartId).GetAwaiter().GetResult().Value;
            return new ApplicationResponse<Order> { IsSuccess = true, Value = order };
        }

        public async Task<ApplicationResponse<bool>> SyncOrderWithCartAsync(int orderId, int cartId)
        {
            var order = _orderRepository.GetOrderByIdAsync(orderId).GetAwaiter().GetResult().Value;
            if (order == null || order.Status != OrderStatus.Pending || order.CartId != cartId)
            {
                return new ApplicationResponse<bool> { IsSuccess = true, Value = false };
            }

            var cart = _cartService.GetCartByIdAsync(cartId, CartStatus.Active).GetAwaiter().GetResult().Value;
            if (cart == null)
            {
                cart = _cartService.GetCartByIdAsync(cartId, CartStatus.PendingPayment).GetAwaiter().GetResult().Value;
            }

            if (cart == null || cart.CartItems == null || !cart.CartItems.Any())
            {
                return new ApplicationResponse<bool> { IsSuccess = true, Value = false };
            }

            decimal subTotal = cart.CartItems.Sum(item => item.UnitPrice * item.Quantity);
            decimal discountTotal = Math.Clamp(cart.CartCoupons?.Sum(coupon => coupon.DiscountAmount) ?? 0, 0, subTotal);
            decimal taxAmount = PricingPolicy.Tax(subTotal, discountTotal);
            decimal shippingAmount = PricingPolicy.Shipping(subTotal);
            decimal total = subTotal - discountTotal + taxAmount + shippingAmount;

            order.SubTotal = subTotal;
            order.DiscountTotal = discountTotal;
            order.TaxAmount = taxAmount;
            order.ShippingAmount = shippingAmount;
            order.Total = total;
            order.UpdatedAt = DateTime.UtcNow;

            await _orderRepository.DeleteOrderItemsAsync(orderId);

            foreach (var cartItem in cart.CartItems)
            {
                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = cartItem.ProductId,
                    ProductName = cartItem.Product?.Name ?? "Unknown Product",
                    Quantity = cartItem.Quantity,
                    UnitPrice = cartItem.UnitPrice,
                    TotalPrice = cartItem.UnitPrice * cartItem.Quantity,
                    DiscountAmount = 0
                };

                await _orderRepository.CreateOrderItemAsync(orderItem);
            }

            return new ApplicationResponse<bool> { IsSuccess = true, Value = await _orderRepository.UpdateOrderAsync(order) };
        }

        public async Task<ApplicationResponse<bool>> DeductInventoryForPaidOrderAsync(int orderId)
        {
            var order = _orderRepository.GetOrderByIdAsync(orderId).GetAwaiter().GetResult().Value;
            if (order == null || order.OrderItems == null || !order.OrderItems.Any())
            {
                _logger.LogWarning("DeductInventoryForPaidOrderAsync: order {OrderId} missing or has no items", orderId);
                return new ApplicationResponse<bool> { IsSuccess = true, Value = false };
            }

            if (order.Status != OrderStatus.Pending)
            {
                return new ApplicationResponse<bool> { IsSuccess = true, Value = true };
            }

            foreach (var item in order.OrderItems)
            {
                var ok = _inventoryService.ConfirmStockDeductionAsync(
                    item.ProductId,
                    item.Quantity,
                    orderId,
                    "Order").GetAwaiter().GetResult().Value;

                if (!ok)
                {
                    _logger.LogError(
                        "ConfirmStockDeduction failed for order {OrderId}, product {ProductId}, qty {Quantity}",
                        orderId,
                        item.ProductId,
                        item.Quantity);
                    return new ApplicationResponse<bool> { IsSuccess = true, Value = false };
                }
            }

            return new ApplicationResponse<bool> { IsSuccess = true, Value = true };
        }
    }
}
