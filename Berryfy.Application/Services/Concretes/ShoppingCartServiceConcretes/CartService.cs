using AutoMapper;
using Berryfy.Application.Services.Interfaces.CouponServiceInterfaces;
using Berryfy.Application.Services.Interfaces.InventoryServiceInterfaces;
using Berryfy.Application.Services.Interfaces.ProductServiceInterfaces;
using Berryfy.Application.Services.Interfaces.ShoppingCartServiceInterfaces;
using Berryfy.Domain.Constants;
using Berryfy.Domain.Entities.ProductEntities;
using Berryfy.Domain.Entities.ShoppingCartEntities;
using Berryfy.Domain.Repositories;
using Berryfy.Domain.Repositories.OrderInterfaces;
using Berryfy.Domain.Repositories.ShoppingCartInterfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Berryfy.Application.Dtos.CouponDtos.Responses;
using Berryfy.Application.Dtos.ShoppingCartDtos.Responses;
using Berryfy.Application.Dtos.ProductDtos.Responses;

namespace Berryfy.Application.Services.Concretes.ShoppingCartServiceConcretes
{
    public class CartService : ICartService
    {

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICartRepository _cartRepository;
        private readonly ILogger<CartService> _logger;
        private readonly IProductService _productService;
        private readonly IInventoryService _inventoryService;
        private readonly ICouponService _couponService;
        private readonly IUserCouponService _userCouponService;
        private readonly IOrderRepository _orderRepository;

        public CartService(
                           ICartRepository cartRepository,
                           ILogger<CartService> logger,
                           IConfiguration configuration,
                           IInventoryService inventoryService,
                           IProductService productService,
                           ICouponService couponService,
                           IUserCouponService userCouponService,
                           IOrderRepository orderRepository
                           )
        {
            _unitOfWork = unitOfWork;
            _cartRepository = cartRepository;
            _logger = logger;
            _inventoryService = inventoryService;
            _productService = productService;
            _couponService = couponService;
            _userCouponService = userCouponService;
            _orderRepository = orderRepository;
        }

        private static decimal GetPercentageRate(CouponResponse coupon)
        {
            if (coupon.Value > 0)
            {
                return coupon.Value <= 1m ? coupon.Value : coupon.Value / 100m;
            }

            if (coupon.DiscountAmount > 0)
            {
                return coupon.DiscountAmount / 100m;
            }

            return 0;
        }

        private static decimal GetFixedDiscountAmount(CouponResponse coupon)
        {
            if (coupon.Value > 0)
            {
                return coupon.Value;
            }

            return coupon.DiscountAmount > 0 ? coupon.DiscountAmount : 0;
        }

        private static decimal ComputePercentageDiscount(CouponResponse coupon, decimal cartSubTotal)
        {
            var rate = GetPercentageRate(coupon);
            return rate > 0
                ? Math.Round(cartSubTotal * rate, 2, MidpointRounding.AwayFromZero)
                : 0;
        }

        private static decimal ComputeFixedDiscount(CouponResponse coupon, decimal cartSubTotal)
        {
            var amt = GetFixedDiscountAmount(coupon);
            return amt > 0 ? Math.Min(amt, cartSubTotal) : 0;
        }

        private static decimal ComputeDiscountAmount(CouponResponse coupon, decimal cartSubTotal)
        {
            if (cartSubTotal <= 0)
            {
                return 0;
            }

            return coupon.Type switch
            {
                CouponType.Percentage => ComputePercentageDiscount(coupon, cartSubTotal),
                CouponType.FixedAmount => ComputeFixedDiscount(coupon, cartSubTotal),
                _ => 0
            };
        }


        public async Task<CartResponse> GetCartByUserIdAsync(int userId, CartStatus? status = CartStatus.Active)
        {
            if (userId <= 0)
            {
                return null;
            }
            var dbCart = await _cartRepository.GetCartByUserIdAsync(userId, status);

            if (dbCart == null)
            {
                return null;
            }

            var mappedCart = CartResponse.MapFromCart(dbCart);

            return mappedCart;
        }
        public async Task<CartResponse> GetCartBySessionIdAsync(string sessionId, CartStatus? status = CartStatus.Active)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                return null;
            }

            var dbCart = await _cartRepository.GetCartBySessionIdAsync(sessionId, status);

            if (dbCart == null)
            {
                await CreateCartAsync(null, sessionId);
                dbCart = await _cartRepository.GetCartBySessionIdAsync(sessionId, status);
            }

            return CartResponse.MapFromCart(dbCart);
        }

        public async Task MergeCartAsync(int userId, string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return;
            }

            var sessionCart = await _cartRepository.GetCartBySessionIdAsync(sessionId, CartStatus.Active);
            if (sessionCart == null || sessionCart.CartItems == null || sessionCart.CartItems.Count == 0)
            {
                return;
            }

            var guestItems = sessionCart.CartItems.Where(i => i.ShoppingCartId == sessionCart.Id).ToList();
            if (guestItems.Count == 0)
            {
                return;
            }

            var userCart = await _cartRepository.GetCartByUserIdAsync(userId, CartStatus.Active);

            if (userCart == null)
            {
                sessionCart.UserId = userId;
                sessionCart.SessionId = null;
                foreach (var item in sessionCart.CartItems)
                {
                    item.UserId = userId;
                    item.SessionId = null;
                }

                await _cartRepository.UpdateCartAsync(sessionCart);
                _logger.LogInformation("Merged guest cart {CartId} into user {UserId} (took over guest cart)", sessionCart.Id, userId);
                return;
            }

            foreach (var guestItem in guestItems)
            {
                var userCartFresh = await _cartRepository.GetCartByUserIdAsync(userId, CartStatus.Active);
                if (userCartFresh == null)
                {
                    _logger.LogWarning("User {UserId} lost active cart during merge; stopping", userId);
                    break;
                }

                var productId = guestItem.ProductId;
                var guestQty = guestItem.Quantity;
                var unitPrice = guestItem.UnitPrice;
                var userLine = userCartFresh.CartItems?.FirstOrDefault(i => i.ProductId == productId);

                var released = await _inventoryService.ReleaseReservedStockAsync(productId, guestQty, sessionCart.Id, "CartItem");
                if (!released)
                {
                    _logger.LogWarning("Could not release guest reservation for product {ProductId} on cart {CartId}", productId, sessionCart.Id);
                    continue;
                }

                var reserved = await _inventoryService.ReserveStockAsync(productId, guestQty, userCartFresh.Id, "CartItem");
                if (!reserved)
                {
                    _logger.LogWarning("Could not reserve product {ProductId} on user cart {CartId}; restoring guest reservation", productId, userCartFresh.Id);
                    await _inventoryService.ReserveStockAsync(productId, guestQty, sessionCart.Id, "CartItem");
                    continue;
                }

                if (userLine != null)
                {
                    var combined = userLine.Quantity + guestQty;
                    await _cartRepository.UpdateItemQuantityAsync(userId, null, productId, combined);
                }
                else
                {
                    await _cartRepository.CreateItemAsync(userCartFresh.Id, userId, null, productId, guestQty, unitPrice);
                }

                await _cartRepository.RemoveItemAsync(null, sessionId, productId);
            }

            var leftover = await _cartRepository.GetCartByIdAsync(sessionCart.Id, CartStatus.Active);
            if (leftover?.CartItems == null || leftover.CartItems.Count == 0)
            {
                await _cartRepository.DeleteCartById(sessionCart.Id);
            }

            _logger.LogInformation("Merged guest session {SessionId} into user {UserId} cart {UserCartId}", sessionId, userId, userCart.Id);
        }

        public async Task<CartResponse> GetCartByIdAsync(int cartId, CartStatus status)
        {

            var dbCart = await _cartRepository.GetCartByIdAsync(cartId, status);

            if (dbCart == null)
            {
                return null;
            }

            var mappedCart = CartResponse.MapFromCart(dbCart);

            return mappedCart;
        }


        public async Task<CartResponse> CreateCartAsync(int? userId, string? sessionId)
        {
            CartResponse? cart = null;

            if (userId.HasValue)
            {
                cart = CartResponse.MapFromCart(await _cartRepository.GetCartByUserIdAsync(userId, CartStatus.Active));

                if (cart != null)
                {
                    return cart;
                }

                else
                {
                    var createdCart = await _cartRepository.CreateCartAsync(userId, CartStatus.Active);
                    return CartResponse.MapFromCart(createdCart);
                }

            }

            else if (!string.IsNullOrEmpty(sessionId))
            {

                cart = CartResponse.MapFromCart(await _cartRepository.GetCartBySessionIdAsync(sessionId, CartStatus.Active));

                if (cart != null)
                {
                    return cart;
                }
                else
                {
                    var createdCart = await _cartRepository.CreateCartAsync(sessionId, CartStatus.Active);
                    return CartResponse.MapFromCart(createdCart);
                }
            }

            else
            {
                return null;
            }
        }
        public async Task<CartResponse?> AddItemAsync(int cartId, int? userId, string? sessionId, int productId, int quantity)
        {
            try
            {
                _logger.LogDebug("Adding item to cart: CartId={CartId}, ProductId={ProductId}, Quantity={Quantity}",
                    cartId, productId, quantity);

                if (quantity <= 0)
                {
                    _logger.LogWarning("Invalid quantity {Quantity} for product {ProductId}", quantity, productId);
                    return null;
                }

                var product = await _productService.GetByIdAsync(productId);
                var mappedProduct = ProductResponse.MapToProduct(product);

                if (mappedProduct == null)
                {
                    _logger.LogWarning("Product not found: {ProductId}", productId);
                    return null;
                }

                if (!mappedProduct.IsActive)
                {
                    _logger.LogWarning("Product {ProductId} is not active", productId);
                    return null;
                }

                var existingItem = await GetItemAsync(cartId, productId);
                int totalQuantityNeeded = quantity;

                if (existingItem != null)
                {
                    totalQuantityNeeded = checked(existingItem.Quantity + quantity);
                    _logger.LogDebug("Existing item found. Current quantity: {CurrentQuantity}, Adding: {AddQuantity}, Total needed: {TotalQuantity}",
                        existingItem.Quantity, quantity, totalQuantityNeeded);
                }


                // Existing units are already reserved and excluded from available stock.
                if (!await _inventoryService.IsInStockAsync(productId, quantity))
                {
                    _logger.LogWarning("Insufficient stock for product {ProductId}. Requested: {Quantity}, Total needed: {TotalQuantity}",
                        productId, quantity, totalQuantityNeeded);
                    return null;
                }

                var stockReserved = await _inventoryService.ReserveStockAsync(
                    productId,
                    quantity,
                    cartId,
                    "CartItem");

                if (!stockReserved)
                {
                    _logger.LogError("Failed to reserve stock for product {ProductId}, quantity {Quantity}", productId, quantity);
                    return null;
                }

                Cart updatedCart = null;

                try
                {
                    if (existingItem != null)
                    {
                        _logger.LogDebug("Updating existing cart item quantity");
                        updatedCart = await _cartRepository.UpdateItemQuantityAsync(userId, sessionId, productId, totalQuantityNeeded);
                    }
                    else
                    {
                        _logger.LogDebug("Creating new cart item");
                        var createdItem = await _cartRepository.CreateItemAsync(cartId, userId, sessionId, productId, quantity, product.Price);

                        if (createdItem == null)
                        {
                            throw new InvalidOperationException("Failed to create cart item");
                        }

                        if (userId.HasValue)
                        {
                            updatedCart = await _cartRepository.GetCartByUserIdAsync(userId, CartStatus.Active);
                            if (updatedCart == null)
                            {
                                updatedCart = await _cartRepository.GetCartByUserIdAsync(userId, CartStatus.PendingPayment);
                            }
                        }
                        else if (!string.IsNullOrEmpty(sessionId))
                        {
                            updatedCart = await _cartRepository.GetCartBySessionIdAsync(sessionId, CartStatus.Active);
                            if (updatedCart == null)
                            {
                                updatedCart = await _cartRepository.GetCartBySessionIdAsync(sessionId, CartStatus.PendingPayment);
                            }
                        }
                    }

                    if (updatedCart == null)
                    {
                        throw new InvalidOperationException("Failed to retrieve updated cart");
                    }

                    _logger.LogInformation("Successfully added item to cart: CartId={CartId}, ProductId={ProductId}, Quantity={Quantity}",
                        cartId, productId, quantity);

                    return CartResponse.MapFromCart(updatedCart);
                }
                catch
                {
                    _logger.LogWarning("Cart operation failed, releasing reserved stock for product {ProductId}, quantity {Quantity}",
                        productId, quantity);

                    await _inventoryService.ReleaseReservedStockAsync(
                        productId,
                        quantity,
                        cartId,
                        "CartItem");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to cart: CartId={CartId}, ProductId={ProductId}, Quantity={Quantity}",
                    cartId, productId, quantity);
                return null;
            }
        }
        public async Task<CartResponse> UpdateItemQuantityAsync(int cartId, int? userId, string? sessionId, int productId, int quantity)
        {

            if (quantity <= 0)
            {
                return null;
            }

            Cart? cart = null;

            if (userId.HasValue)
            {
                cart = await _cartRepository.GetCartByUserIdAsync(userId, CartStatus.Active);
                if (cart == null)
                {
                    cart = await _cartRepository.GetCartByUserIdAsync(userId, CartStatus.PendingPayment);
                }
            }

            else if (!string.IsNullOrEmpty(sessionId))
            {
                cart = await _cartRepository.GetCartBySessionIdAsync(sessionId, CartStatus.Active);
                if (cart == null)
                {
                    cart = await _cartRepository.GetCartBySessionIdAsync(sessionId, CartStatus.PendingPayment);
                }
            }

            else
            {
                return null;
            }

            if (cart == null || cart.Id != cartId) return null;
            var item = cart.CartItems.FirstOrDefault(i => i.ProductId == productId);

            if (item == null)
            {
                return null;
            }

            int quantityDifference = quantity - item.Quantity;

            if (quantityDifference == 0)
            {
                return CartResponse.MapFromCart(cart);
            }

            if (quantityDifference > 0)
            {
                if (!await _inventoryService.IsInStockAsync(productId, quantityDifference))
                {
                    return null;
                }

                if (!await _inventoryService.ReserveStockAsync(
                    productId,
                    quantityDifference,
                    cartId,
                    "CartItem")) return null;
            }
            else
            {
                if (!await _inventoryService.ReleaseReservedStockAsync(
                    productId,
                    Math.Abs(quantityDifference),
                    cartId,
                    "CartItem")) return null;
            }

            var updatedQuantity = await _cartRepository.UpdateItemQuantityAsync(userId, sessionId, productId, quantity);


            if (updatedQuantity == null && quantityDifference > 0)
            {
                await _inventoryService.ReleaseReservedStockAsync(
                    productId,
                    quantityDifference,
                    cartId,
                    "CartItem");
                return null;
            }

            return CartResponse.MapFromCart(updatedQuantity);
        }


        public async Task<bool> RemoveItemAsync(int cartId, int? userId, string? sessionId, int productId)
        {
            try
            {
                _logger.LogDebug("Removing item from cart: CartId={CartId}, ProductId={ProductId}", cartId, productId);

                Cart cart = null;

                if (userId.HasValue)
                {
                    cart = await _cartRepository.GetCartByUserIdAsync(userId, CartStatus.Active);
                    if (cart == null)
                    {
                        cart = await _cartRepository.GetCartByUserIdAsync(userId, CartStatus.PendingPayment);
                    }
                }
                else if (!string.IsNullOrEmpty(sessionId))
                {
                    cart = await _cartRepository.GetCartByIdAsync(cartId, CartStatus.Active);
                    if (cart == null)
                    {
                        cart = await _cartRepository.GetCartByIdAsync(cartId, CartStatus.PendingPayment);
                    }
                }
                else
                {
                    _logger.LogWarning("No userId or sessionId provided for cart item removal");
                    return false;
                }

                if (cart == null)
                {
                    _logger.LogWarning("Cart not found for removal: CartId={CartId}", cartId);
                    return false;
                }

                var item = cart.CartItems.FirstOrDefault(i => i.ProductId == productId);

                if (item == null)
                {
                    _logger.LogWarning("Cart item not found: CartId={CartId}, ProductId={ProductId}", cartId, productId);
                    return false;
                }

                var quantityToRelease = item.Quantity;
                _logger.LogDebug("Releasing reserved stock: ProductId={ProductId}, Quantity={Quantity}", productId, quantityToRelease);

                await _inventoryService.ReleaseReservedStockAsync(
                    productId,
                    quantityToRelease,
                    cartId,
                    "CartItem");

                var removedItem = await _cartRepository.RemoveItemAsync(userId, sessionId, productId);

                if (removedItem)
                {
                    _logger.LogInformation("Successfully removed item from cart: CartId={CartId}, ProductId={ProductId}, ReleasedQuantity={Quantity}",
                        cartId, productId, quantityToRelease);
                }
                else
                {
                    _logger.LogError("Failed to remove item from cart: CartId={CartId}, ProductId={ProductId}", cartId, productId);

                    await _inventoryService.ReserveStockAsync(
                        productId,
                        quantityToRelease,
                        cartId,
                        "CartItem");
                }

                return removedItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item from cart: CartId={CartId}, ProductId={ProductId}", cartId, productId);
                return false;
            }
        }


        public async Task<bool> ClearCartAsync(int cartId, int? userId, string? sessionId)
        {
            try
            {
                _logger.LogDebug("Clearing cart: CartId={CartId}", cartId);

                if (cartId <= 0)
                {
                    _logger.LogWarning("Invalid cart ID provided: {CartId}", cartId);
                    return false;
                }

                
                Cart cart = await _cartRepository.GetCartByIdAsync(cartId, CartStatus.Active);
                if (cart == null)
                {
                    cart = await _cartRepository.GetCartByIdAsync(cartId, CartStatus.PendingPayment);
                }
                if (cart == null)
                {
                    cart = await _cartRepository.GetCartByIdAsync(cartId, CartStatus.Converted);
                }

                if (cart == null)
                {
                    _logger.LogWarning("Cart not found for clearing: CartId={CartId}", cartId);
                    return false;
                }

                
                if (cart.UserId.HasValue ? cart.UserId != userId :
                    string.IsNullOrEmpty(sessionId) || cart.SessionId != sessionId)
                {
                    _logger.LogWarning("User {UserId} attempted to clear cart {CartId} owned by user {OwnerId}",
                        userId, cartId, cart.UserId);
                    return false;
                }

                bool isConverted = cart.Status == CartStatus.Converted;

                if (cart.CartItems?.Any() == true && !isConverted)
                {
                    _logger.LogDebug("Releasing reserved stock for {ItemCount} items in cart {CartId}", cart.CartItems.Count, cartId);

                    foreach (var item in cart.CartItems)
                    {
                        try
                        {
                            var released = await _inventoryService.ReleaseReservedStockAsync(
                                item.ProductId,
                                item.Quantity,
                                cartId,
                                "CartItem");
                            if (!released) return false;

                            _logger.LogDebug("Released stock for product {ProductId}, quantity {Quantity}",
                                item.ProductId, item.Quantity);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to release stock for product {ProductId} in cart {CartId}",
                                item.ProductId, cartId);
                            return false;
                        }
                    }
                }
                else if (isConverted)
                {
                    _logger.LogDebug("Cart {CartId} is converted - skipping stock release (inventory already deducted)", cartId);
                }

                cart.CartItems.Clear();
                cart.CartCoupons.Clear();
                cart.UpdatedAt = DateTime.UtcNow;

                var updatedCart = await _cartRepository.UpdateCartAsync(cart);
                var success = updatedCart != null;

                if (success)
                {
                    _logger.LogInformation("Successfully cleared cart {CartId}", cartId);
                }
                else
                {
                    _logger.LogError("Failed to clear cart {CartId}", cartId);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart: CartId={CartId}", cartId);
                return false;
            }
        }

        public async Task<bool> CompleteCartAsync(int cartId, int? userId)
        {
            var cart = await _cartRepository.GetCartByIdAsync(cartId, CartStatus.PendingPayment);
            if (cart == null || !userId.HasValue || cart.UserId != userId) return false;
            var order = await _orderRepository.GetOrderByCartIdAsync(cartId);
            if (order == null || !order.isPaid) return false;
            return await ConvertCartAsync(cartId);
        }

        public async Task<bool> ConvertCartAsync(int cartId)
        {
            try
            {
                _logger.LogInformation("Starting cart conversion for cart ID: {CartId}", cartId);

                var cart = await _cartRepository.GetCartByIdAsync(cartId, CartStatus.PendingPayment);
                if (cart == null)
                {
                    _logger.LogWarning("Cart not found or not in PendingPayment status for conversion: {CartId}", cartId);
                    return false;
                }

                if (cart.CartItems == null || !cart.CartItems.Any())
                {
                    _logger.LogWarning("Cannot convert empty cart: {CartId}", cartId);
                    return false;
                }

                cart.Status = CartStatus.Converted;
                cart.UpdatedAt = DateTime.UtcNow;

                var updatedCart = await _cartRepository.UpdateCartAsync(cart);
                if (updatedCart == null)
                {
                    _logger.LogError("Failed to update cart status to Converted for cart {CartId}", cartId);
                    return false;
                }

                _logger.LogInformation("Successfully converted cart {CartId} status to Converted", cartId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during cart conversion for cart {CartId}", cartId);
                return false;
            }
        }

        public async Task<bool> UpdateCartStatusAsync(int cartId, CartStatus status)
        {
            try
            {
                _logger.LogInformation("Updating cart {CartId} status to {Status}", cartId, status);

                var cart = await _cartRepository.GetCartByIdAsync(cartId, CartStatus.Active);
                if (cart == null)
                {
                    cart = await _cartRepository.GetCartByIdAsync(cartId, CartStatus.PendingPayment);
                }

                if (cart == null)
                {
                    _logger.LogWarning("Cart not found: {CartId}", cartId);
                    return false;
                }

                if (cart.Status == status)
                {
                    return true;
                }

                cart.Status = status;
                cart.UpdatedAt = DateTime.UtcNow;

                var updatedCart = await _cartRepository.UpdateCartAsync(cart);
                if (updatedCart == null)
                {
                    _logger.LogError("Failed to update cart status for cart {CartId}", cartId);
                    return false;
                }

                _logger.LogInformation("Successfully updated cart {CartId} status to {Status}", cartId, status);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart status for cart {CartId}", cartId);
                return false;
            }
        }

        public async Task<bool> ReactivateCartAsync(int cartId, int orderId)
        {
            try
            {
                _logger.LogInformation("Reactivating cart {CartId} from order {OrderId}", cartId, orderId);

                var cart = await _cartRepository.GetCartByIdAsync(cartId, CartStatus.PendingPayment);
                if (cart == null)
                {
                    _logger.LogWarning("Cart not found or not in PendingPayment status: {CartId}", cartId);
                    return false;
                }

                cart.Status = CartStatus.Active;
                cart.UpdatedAt = DateTime.UtcNow;

                var updatedCart = await _cartRepository.UpdateCartAsync(cart);
                if (updatedCart == null)
                {
                    _logger.LogError("Failed to reactivate cart {CartId}", cartId);
                    return false;
                }

                _logger.LogInformation("Successfully reactivated cart {CartId}", cartId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reactivating cart {CartId}", cartId);
                return false;
            }
        }

        public async Task<CartResponse> RefreshCartAsync(int cartId)
        {
            return await GetCartByIdAsync(cartId, CartStatus.Active)
                ?? await GetCartByIdAsync(cartId, CartStatus.PendingPayment);
        }


        public async Task<CartItemResponse> GetItemAsync(int cartId, int productId)
        {
            CartResponse? cart = null;

            cart = await GetCartByIdAsync(cartId, CartStatus.Active);
            if (cart == null)
            {
                cart = await GetCartByIdAsync(cartId, CartStatus.PendingPayment);
            }

            if (cart == null)
            {
                return null;
            }

            var item = cart.CartItems.Where(i => i.ProductId == productId).FirstOrDefault();

            if (item == null)
            {
                return null;
            }

            return item;
        }


        public async Task<CartResponse> ApplyCouponAsync(int cartId, int? userId, string couponCode)
        {
            try
            {

                var cart = await _cartRepository.GetCartByIdAsync(cartId, CartStatus.Active);
                if (cart == null)
                {
                    cart = await _cartRepository.GetCartByIdAsync(cartId, CartStatus.PendingPayment);
                }

                if (cart == null || !userId.HasValue || cart.UserId != userId)
                {
                    return null;
                }

                if (string.IsNullOrWhiteSpace(couponCode))
                {
                    return null;
                }

                if (cart.CartCoupons.Any(cc => cc.Coupon.Code == couponCode))
                {
                    return null;
                }

                if (!userId.HasValue || userId.Value <= 0)
                {
                    return null;
                }

                var coupon = await _couponService.GetByCodeAsync(couponCode);

                if (coupon == null || !coupon.IsActive || await _userCouponService.IsCouponUsedByUser(userId.Value, couponCode))
                {
                    return null;
                }

                var assignedCoupons = await _userCouponService.GetCouponsByUserIdAsync(userId.Value);
                if (assignedCoupons == null || !assignedCoupons.Any(c => c.Id == coupon.Id))
                {
                    return null;
                }

                if (!cart.CartItems.Any())
                {
                    return null;
                }

                if (coupon.IsForNewUsersOnly && await _orderRepository.UserHasPaidOrderAsync(userId.Value))
                {
                    return null;
                }

                decimal cartSubTotal = cart.SubTotal;

                if (coupon.MinimumOrderAmount > 0 && cartSubTotal < coupon.MinimumOrderAmount)
                {
                    return null;
                }

                var discountAmount = ComputeDiscountAmount(coupon, cartSubTotal);
                if (discountAmount <= 0)
                {
                    return null;
                }

                var cartCoupon = new CartCoupon
                {
                    CartId = cart.Id,
                    CouponId = coupon.Id,
                    UserId = userId,
                    DiscountAmount = discountAmount,
                    AppliedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                cart.CartCoupons.Add(cartCoupon);

                var updatedCart = await _cartRepository.UpdateCartAsync(cart);
                if (updatedCart == null)
                {
                    return null;
                }

                return _mapper.Map<CartResponse>(updatedCart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying coupon {CouponCode} to cart {CartId}", couponCode, cartId);
                return null;
            }
        }

        public async Task<CartResponse> RemoveCouponAsync(int cartId, int? userId, string? sessionId, int couponId)
        {
            try
            {
                var cart = await _cartRepository.GetCartByIdAsync(cartId, CartStatus.Active)
                    ?? await _cartRepository.GetCartByIdAsync(cartId, CartStatus.PendingPayment);
                if (cart == null || (userId.HasValue
                    ? cart.UserId != userId
                    : cart.UserId.HasValue || string.IsNullOrWhiteSpace(sessionId) || cart.SessionId != sessionId))
                {
                    return null;
                }

                var cartCoupon = cart.CartCoupons.FirstOrDefault(cc => cc.CouponId == couponId);
                if (cartCoupon == null)
                {
                    return null;
                }

                cart.CartCoupons.Remove(cartCoupon);

                var updatedCart = await _cartRepository.UpdateCartAsync(cart);
                if (updatedCart == null)
                {
                    throw new InvalidOperationException("Failed to remove coupon from cart");
                }

                return _mapper.Map<CartResponse>(updatedCart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing coupon {CouponId} from cart {CartId}", couponId, cartId);
                throw;
            }
        }

        public async Task<bool> HandleAbandonedCartAsync(int cartId)
        {
            try
            {
                _logger.LogInformation("Handling abandoned cart: {CartId}", cartId);

                var cart = await _cartRepository.GetCartByIdAsync(cartId, CartStatus.Active);
                if (cart == null)
                {
                    _logger.LogWarning("Cart not found or not active: {CartId}", cartId);
                    return false;
                }

                if (cart.CartItems?.Any() == true)
                {
                    _logger.LogDebug("Releasing reserved stock for abandoned cart {CartId} with {ItemCount} items",
                        cartId, cart.CartItems.Count);

                    var stockReleaseTasks = cart.CartItems.Select(async item =>
                    {
                        try
                        {
                            await _inventoryService.ReleaseReservedStockAsync(
                                item.ProductId,
                                item.Quantity,
                                cartId,
                                "CartItem");

                            _logger.LogDebug("Released stock for abandoned cart - Product: {ProductId}, Quantity: {Quantity}",
                                item.ProductId, item.Quantity);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to release stock for product {ProductId} in abandoned cart {CartId}",
                                item.ProductId, cartId);
                        }
                    });

                    await Task.WhenAll(stockReleaseTasks);
                }

                cart.Status = CartStatus.Abandoned;
                cart.UpdatedAt = DateTime.UtcNow;

                var updatedCart = await _cartRepository.UpdateCartAsync(cart);
                var success = updatedCart != null;

                if (success)
                {
                    _logger.LogInformation("Successfully handled abandoned cart {CartId}", cartId);
                }
                else
                {
                    _logger.LogError("Failed to update abandoned cart status: {CartId}", cartId);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling abandoned cart: {CartId}", cartId);
                return false;
            }
        }

        public async Task<int> CleanupExpiredCartsAsync()
        {
            try
            {
                _logger.LogInformation("Starting cleanup of expired carts");

                var allCarts = await _cartRepository.GetCartsAsync();
                var expiredCarts = allCarts.Where(c =>
                    c.Status == CartStatus.Active &&
                    c.UpdatedAt < DateTime.UtcNow.AddHours(-24))
                    .ToList();

                if (!expiredCarts.Any())
                {
                    _logger.LogInformation("No expired carts found for cleanup");
                    return 0;
                }

                _logger.LogInformation("Found {ExpiredCartCount} expired carts for cleanup", expiredCarts.Count);

                int cleanedUpCount = 0;
                var cleanupTasks = expiredCarts.Select(async cart =>
                {
                    try
                    {
                        var success = await HandleAbandonedCartAsync(cart.Id);
                        if (success)
                        {
                            Interlocked.Increment(ref cleanedUpCount);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error cleaning up expired cart: {CartId}", cart.Id);
                    }
                });

                await Task.WhenAll(cleanupTasks);

                _logger.LogInformation("Completed cleanup of expired carts. Cleaned up: {CleanedUpCount}/{TotalExpired}",
                    cleanedUpCount, expiredCarts.Count);

                return cleanedUpCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during expired cart cleanup");
                return 0;
            }
        }
    }
}
