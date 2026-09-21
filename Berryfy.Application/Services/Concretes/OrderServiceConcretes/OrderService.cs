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


        public async Task<OrderTotal> CalculateOrderTotalsAsync(int cartId)
        {
            var cart = await _cartService.GetCartByIdAsync(cartId, CartStatus.Active)
                ?? await _cartService.GetCartByIdAsync(cartId, CartStatus.PendingPayment);

            if (cart == null)
            {
                return null;
            }

            decimal subTotal = cart.CartItems.Sum(item => item.UnitPrice * item.Quantity);
            decimal discountTotal = Math.Clamp(cart.CartCoupons?.Sum(coupon => coupon.DiscountAmount) ?? 0, 0, subTotal);
            decimal taxAmount = PricingPolicy.Tax(subTotal, discountTotal);
            decimal shippingAmount = PricingPolicy.Shipping(subTotal);

            return new OrderTotal
            {
                SubTotal = subTotal,
                DiscountTotal = discountTotal,
                TaxAmount = taxAmount,
                ShippingAmount = shippingAmount,
                Total = subTotal - discountTotal + taxAmount + shippingAmount
            };
        }

        public async Task<bool> CancelOrderAsync(int orderId, string reason)
        {
            var result = await _orderCancellationService.CancelOrderAsync(orderId, reason);
            return result.IsSuccess;
        }

        public async Task<Order?> CreateOrderFromCartAsync(int cartId, CreateOrder orderDto)
        {
            var cart = await _cartService.GetCartByIdAsync(cartId, CartStatus.Active);
            if (cart == null)
            {
                cart = await _cartService.GetCartByIdAsync(cartId, CartStatus.PendingPayment);
            }

            if (cart == null || orderDto.UserId <= 0 || cart.UserId != orderDto.UserId)
            {
                return null;
            }

            var existingOrder = await _orderRepository.GetOrderByCartIdAsync(cartId);
            if (existingOrder != null)
            {
                return null;
            }

            if (cart.CartItems == null || !cart.CartItems.Any())
            {
                return null;
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

            order = await _orderRepository.CreateOrderAsync(order);

            if (order == null)
            {
                return null;
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

            return order;
        }

        public async Task<bool> RefundOrderAsync(int orderId, string reason)
        {
            var result = await _refundOrchestrationService.ProcessRefundAsync(orderId, reason);
            return result.IsSuccess;
        }

        public async Task<bool> UpdateOrderStatusAsync(Order order, OrderStatus newStatus)
        {
            if (order == null || !Enum.IsDefined(newStatus)) return false;

            // Settlement and returns must use their dedicated workflows.
            if (newStatus is OrderStatus.Cancelled or OrderStatus.Refunded) return false;
            if (order.Status == newStatus) return true;
            if (!order.isPaid) return false;

            var allowed = (order.Status, newStatus) switch
            {
                (OrderStatus.Pending, OrderStatus.Processing) => true,
                (OrderStatus.Processing, OrderStatus.Shipped) => true,
                (OrderStatus.Shipped, OrderStatus.Delivered) => true,
                (OrderStatus.Delivered, OrderStatus.Completed) => true,
                _ => false
            };
            if (!allowed) return false;

            var saved = await _orderRepository.UpdateOrderStatusAsync(order.Id, newStatus);
            if (saved)
            {
                order.Status = newStatus;
                order.UpdatedAt = DateTime.UtcNow;
                if (newStatus == OrderStatus.Completed) order.CompletedAt = DateTime.UtcNow;
            }
            return saved;
        }

        public async Task<string> GenerateUniqueReferenceNumberAsync()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var randomComponent = new Random().Next(1000, 9999);
            var referenceNumber = $"ORD-{timestamp}-{randomComponent}";

            var existingOrder = await _orderRepository.GetOrderByReferenceNumberAsync(referenceNumber);

            if (existingOrder != null)
            {
                var additionalRandom = new Random().Next(100, 999);
                referenceNumber = $"ORD-{timestamp}-{randomComponent}-{additionalRandom}";
            }

            return referenceNumber;
        }

        public async Task<OrderResponse?> GetOrderByIdAsync(int orderId)
        {
            var order = await _orderRepository.GetOrderByIdAsync(orderId);

            return OrderResponse.MapFromOrder(order);
        }

        public async Task<Order?> GetOrderByReferenceNumberAsync(string referenceNumber)
        {
            var order = await _orderRepository.GetOrderByReferenceNumberAsync(referenceNumber);
            return order;
        }

        public async Task<List<Order>> GetOrdersByStatusAsync(OrderStatus status, int page = 1, int pageSize = 10)
        {
            var orders = await _orderRepository.GetOrdersByStatusAsync(status, page, pageSize);
            return orders;
        }

        public async Task<List<OrderResponse>> GetUserOrdersAsync(int userId, int page = 1, int pageSize = 10)
        {
            var orders = await _orderRepository.GetUserOrdersAsync(userId, page, pageSize);

            var mappedOrder = OrderResponse.MapFromOrder(orders);
            return mappedOrder;
        }

        public async Task<List<OrderResponse>> GetAllOrdersAsync(int page = 1, int pageSize = 50)
        {
            var orders = await _orderRepository.GetAllOrdersAsync(page, pageSize);

            var mappedOrders = OrderResponse.MapFromOrder(orders);
            return mappedOrders;
        }


        public async Task<bool> ProcessOrderAsync(int orderId)
        {
            var order = await _orderRepository.GetOrderByIdAsync(orderId);

            if (order == null)
            {
                return false;
            }

            if (order.Status != OrderStatus.Pending)
            {
                return false;
            }

            var orderStatusUpdated = await UpdateOrderStatusAsync(order, OrderStatus.Processing);

            return orderStatusUpdated;
        }

        public Task<bool> UpdateOrderPaymentStatusAsync(Order order, PaymentStatus paymentStatus)
        {
            return _orderRepository.UpdateOrderPaymentStatusAsync(order.Id, paymentStatus);
        }

        public async Task<Order?> GetOrderByCartIdAsync(int cartId)
        {
            var order = await _orderRepository.GetOrderByCartIdAsync(cartId);
            return order;
        }

        public async Task<bool> SyncOrderWithCartAsync(int orderId, int cartId)
        {
            var order = await _orderRepository.GetOrderByIdAsync(orderId);
            if (order == null || order.Status != OrderStatus.Pending || order.CartId != cartId)
            {
                return false;
            }

            var cart = await _cartService.GetCartByIdAsync(cartId, CartStatus.Active);
            if (cart == null)
            {
                cart = await _cartService.GetCartByIdAsync(cartId, CartStatus.PendingPayment);
            }

            if (cart == null || cart.CartItems == null || !cart.CartItems.Any())
            {
                return false;
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

            return await _orderRepository.UpdateOrderAsync(order);
        }

        public async Task<bool> DeductInventoryForPaidOrderAsync(int orderId)
        {
            var order = await _orderRepository.GetOrderByIdAsync(orderId);
            if (order == null || order.OrderItems == null || !order.OrderItems.Any())
            {
                _logger.LogWarning("DeductInventoryForPaidOrderAsync: order {OrderId} missing or has no items", orderId);
                return false;
            }

            if (order.Status != OrderStatus.Pending)
            {
                return true;
            }

            foreach (var item in order.OrderItems)
            {
                var ok = await _inventoryService.ConfirmStockDeductionAsync(
                    item.ProductId,
                    item.Quantity,
                    orderId,
                    "Order");

                if (!ok)
                {
                    _logger.LogError(
                        "ConfirmStockDeduction failed for order {OrderId}, product {ProductId}, qty {Quantity}",
                        orderId,
                        item.ProductId,
                        item.Quantity);
                    return false;
                }
            }

            return true;
        }
    }
}
