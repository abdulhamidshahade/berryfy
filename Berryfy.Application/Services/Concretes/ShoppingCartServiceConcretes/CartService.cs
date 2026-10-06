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
using Berryfy.Application.Dtos;

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
                           IUnitOfWork unitOfWork,
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


        public async Task<ApplicationResponse<CartResponse>> GetCartByUserIdAsync(int userId, CartStatus? status = CartStatus.Active)
        {
            if (userId <= 0)
            {
                return new ApplicationResponse<CartResponse>
                {
                    Value = null,
                    IsSuccess = false,
                    ErrorMessage = "Invalid user ID"
                };
            }
            var dbCart = _cartRepository.GetCartByUserIdAsync(userId, status).GetAwaiter().GetResult().Value;

            if (dbCart == null)
            {
                return new ApplicationResponse<CartResponse>
                {
                    Value = null,
                    IsSuccess = false,
                    ErrorMessage = "Cart not found"
                };
            }

            var mappedCart = CartResponse.MapFromCart(dbCart);

            return new ApplicationResponse<CartResponse>
            {
                Value = mappedCart,
                IsSuccess = true
            };
        }
        public async Task<ApplicationResponse<CartResponse>> GetCartBySessionIdAsync(string sessionId, CartStatus? status = CartStatus.Active)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                return new ApplicationResponse<CartResponse>
                {
                    Value = null,
                    IsSuccess = false,
                    ErrorMessage = "Invalid session ID"
                };
            }

            var dbCart = _cartRepository.GetCartBySessionIdAsync(sessionId, status).GetAwaiter().GetResult().Value;

            if (dbCart == null)
            {
                await CreateCartAsync(null, sessionId);
                dbCart = _cartRepository.GetCartBySessionIdAsync(sessionId, status).GetAwaiter().GetResult().Value;
            }

            return new ApplicationResponse<CartResponse>
            {
                Value = CartResponse.MapFromCart(dbCart),
                IsSuccess = true
            };
        }

        public async Task<ApplicationResponse<bool>> MergeCartAsync(int userId, string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return new ApplicationResponse<bool>
                {
                    Value = false,
                    IsSuccess = false,
                    ErrorMessage = "Invalid session ID"
                };
            }

            var sessionCart = _cartRepository.GetCartBySessionIdAsync(sessionId, CartStatus.Active).GetAwaiter().GetResult().Value;
            if (sessionCart == null || sessionCart.CartItems == null || sessionCart.CartItems.Count == 0)
            {
                return new ApplicationResponse<bool>
                {
                    Value = false,
                    IsSuccess = false,
                    ErrorMessage = "No items to merge"
                };
            }

            var guestItems = sessionCart.CartItems.Where(i => i.ShoppingCartId == sessionCart.Id).ToList();
            if (guestItems.Count == 0)
            {
                return new ApplicationResponse<bool>
                {
                    Value = false,
                    IsSuccess = false,
                    ErrorMessage = "No items to merge"
                };
            }

            var userCart = _cartRepository.GetCartByUserIdAsync(userId, CartStatus.Active).GetAwaiter().GetResult().Value;

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
                return new ApplicationResponse<bool>
                {
                    Value = true,
                    IsSuccess = true
                };
            }

            foreach (var guestItem in guestItems)
            {
                var userCartFresh = _cartRepository.GetCartByUserIdAsync(userId, CartStatus.Active).GetAwaiter().GetResult().Value;
                if (userCartFresh == null)
                {
                    _logger.LogWarning("User {UserId} lost active cart during merge; stopping", userId);
                    break;
                }

                var productId = guestItem.ProductId;
                var guestQty = guestItem.Quantity;
                var unitPrice = guestItem.UnitPrice;
                var userLine = userCartFresh.CartItems?.FirstOrDefault(i => i.ProductId == productId);

                var released = _inventoryService.ReleaseReservedStockAsync(productId, guestQty, sessionCart.Id, "CartItem").GetAwaiter().GetResult().Value;
                if (!released)
                {
                    _logger.LogWarning("Could not release guest reservation for product {ProductId} on cart {CartId}", productId, sessionCart.Id);
                    continue;
                }

                var reserved = _inventoryService.ReserveStockAsync(productId, guestQty, userCartFresh.Id, "CartItem").GetAwaiter().GetResult().Value;
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

            var leftover = _cartRepository.GetCartByIdAsync(sessionCart.Id, CartStatus.Active).GetAwaiter().GetResult().Value;
            if (leftover?.CartItems == null || leftover.CartItems.Count == 0)
            {
                await _cartRepository.DeleteCartById(sessionCart.Id);
            }

            return new ApplicationResponse<bool>
            {
                Value = true,
                IsSuccess = true
            };

            _logger.LogInformation("Merged guest session {SessionId} into user {UserId} cart {UserCartId}", sessionId, userId, userCart.Id);
        }

        public async Task<ApplicationResponse<CartResponse>> GetCartByIdAsync(int cartId, CartStatus status)
        {

            var dbCart = _cartRepository.GetCartByIdAsync(cartId, status).GetAwaiter().GetResult().Value;

            if (dbCart == null)
            {
                return new ApplicationResponse<CartResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Cart not found"
                };
            }

            var mappedCart = CartResponse.MapFromCart(dbCart);

            return new ApplicationResponse<CartResponse>
            {
                IsSuccess = true,
                Value = mappedCart
            };
        }


        public async Task<ApplicationResponse<CartResponse>> CreateCartAsync(int? userId, string? sessionId)
        {
            CartResponse? cart = null;

            if (userId.HasValue)
            {
                cart = CartResponse.MapFromCart(_cartRepository.GetCartByUserIdAsync(userId, CartStatus.Active).GetAwaiter().GetResult().Value);

                if (cart != null)
                {
                    return new ApplicationResponse<CartResponse>
                    {
                        IsSuccess = true,
                        Value = cart
                    };
                }

                else
                {
                    var createdCart = _cartRepository.CreateCartAsync(userId, CartStatus.Active).GetAwaiter().GetResult().Value;
                    return new ApplicationResponse<CartResponse>
                    {
                        IsSuccess = true,
                        Value = CartResponse.MapFromCart(createdCart)
                    };
                }

            }

            else if (!string.IsNullOrEmpty(sessionId))
            {

                cart = CartResponse.MapFromCart(_cartRepository.GetCartBySessionIdAsync(sessionId, CartStatus.Active).GetAwaiter().GetResult().Value);

                if (cart != null)
                {
                    return new ApplicationResponse<CartResponse>
                    {
                        IsSuccess = true,
                        Value = cart
                    };
                }
                else
                {
                    var createdCart = _cartRepository.CreateCartAsync(sessionId, CartStatus.Active).GetAwaiter().GetResult().Value;
                    return new ApplicationResponse<CartResponse>
                    {
                        IsSuccess = true,
                        Value = CartResponse.MapFromCart(createdCart)
                    };
                }
            }

            else
            {
                return new ApplicationResponse<CartResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Either userId or sessionId must be provided to create a cart."
                };
            }
        }
        public async Task<ApplicationResponse<CartResponse>?> AddItemAsync(int cartId, int? userId, string? sessionId, int productId, int quantity)
        {
            try
            {
                _logger.LogDebug("Adding item to cart: CartId={CartId}, ProductId={ProductId}, Quantity={Quantity}",
                    cartId, productId, quantity);

                if (quantity <= 0)
                {
                    _logger.LogWarning("Invalid quantity {Quantity} for product {ProductId}", quantity, productId);
                    return new ApplicationResponse<CartResponse>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Invalid quantity provided."
                    };
                }

                var product = _productService.GetByIdAsync(productId).GetAwaiter().GetResult().Value;
                var mappedProduct = ProductResponse.MapToProduct(product);

                if (mappedProduct == null)
                {
                    _logger.LogWarning("Product not found: {ProductId}", productId);
                    return new ApplicationResponse<CartResponse>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Product not found."
                    };
                }

                if (!mappedProduct.IsActive)
                {
                    _logger.LogWarning("Product {ProductId} is not active", productId);
                    return new ApplicationResponse<CartResponse>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Product is not active."
                    };
                }

                var existingItem = GetItemAsync(cartId, productId).GetAwaiter().GetResult().Value;
                int totalQuantityNeeded = quantity;

                if (existingItem != null)
                {
                    totalQuantityNeeded = checked(existingItem.Quantity + quantity);
                    _logger.LogDebug("Existing item found. Current quantity: {CurrentQuantity}, Adding: {AddQuantity}, Total needed: {TotalQuantity}",
                        existingItem.Quantity, quantity, totalQuantityNeeded);
                }


                // Existing units are already reserved and excluded from available stock.
                if (!_inventoryService.IsInStockAsync(productId, quantity).GetAwaiter().GetResult().Value)
                {
                    _logger.LogWarning("Insufficient stock for product {ProductId}. Requested: {Quantity}, Total needed: {TotalQuantity}",
                        productId, quantity, totalQuantityNeeded);
                    return new ApplicationResponse<CartResponse>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Product is not in stock."
                    };
                }

                var stockReserved = _inventoryService.ReserveStockAsync(
                    productId,
                    quantity,
                    cartId,
                    "CartItem").GetAwaiter().GetResult().Value;

                if (!stockReserved)
                {
                    _logger.LogError("Failed to reserve stock for product {ProductId}, quantity {Quantity}", productId, quantity);
                    return new ApplicationResponse<CartResponse>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Failed to reserve stock."
                    };
                }

                Cart updatedCart = null;

                try
                {
                    if (existingItem != null)
                    {
                        _logger.LogDebug("Updating existing cart item quantity");
                        updatedCart = _cartRepository.UpdateItemQuantityAsync(userId, sessionId, productId, totalQuantityNeeded).GetAwaiter().GetResult().Value;
                    }
                    else
                    {
                        _logger.LogDebug("Creating new cart item");
                        var createdItem = _cartRepository.CreateItemAsync(cartId, userId, sessionId, productId, quantity, product.Price).GetAwaiter().GetResult().Value;

                        if (createdItem == null)
                        {
                            throw new InvalidOperationException("Failed to create cart item");
                        }

                        if (userId.HasValue)
                        {
                            updatedCart = _cartRepository.GetCartByUserIdAsync(userId, CartStatus.Active).GetAwaiter().GetResult().Value;
                            if (updatedCart == null)
                            {
                                updatedCart = _cartRepository.GetCartByUserIdAsync(userId, CartStatus.PendingPayment).GetAwaiter().GetResult().Value;
                            }
                        }
                        else if (!string.IsNullOrEmpty(sessionId))
                        {
                            updatedCart = _cartRepository.GetCartBySessionIdAsync(sessionId, CartStatus.Active).GetAwaiter().GetResult().Value;
                            if (updatedCart == null)
                            {
                                updatedCart = _cartRepository.GetCartBySessionIdAsync(sessionId, CartStatus.PendingPayment).GetAwaiter().GetResult().Value;
                            }
                        }
                    }

                    if (updatedCart == null)
                    {
                        throw new InvalidOperationException("Failed to retrieve updated cart");
                    }

                    _logger.LogInformation("Successfully added item to cart: CartId={CartId}, ProductId={ProductId}, Quantity={Quantity}",
                        cartId, productId, quantity);

                    return new ApplicationResponse<CartResponse>()
                    {
                        IsSuccess = true,
                        Value = CartResponse.MapFromCart(updatedCart)
                    };
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
        public async Task<ApplicationResponse<CartResponse>> UpdateItemQuantityAsync(int cartId, int? userId, string? sessionId, int productId, int quantity)
        {

            if (quantity <= 0)
            {
                return new ApplicationResponse<CartResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Quantity must be greater than zero."
                };
            }

            Cart? cart = null;

            if (userId.HasValue)
            {
                cart = _cartRepository.GetCartByUserIdAsync(userId, CartStatus.Active).GetAwaiter().GetResult().Value;
                if (cart == null)
                {
                    cart = _cartRepository.GetCartByUserIdAsync(userId, CartStatus.PendingPayment).GetAwaiter().GetResult().Value;
                }
            }

            else if (!string.IsNullOrEmpty(sessionId))
            {
                cart = _cartRepository.GetCartBySessionIdAsync(sessionId, CartStatus.Active).GetAwaiter().GetResult().Value;
                if (cart == null)
                {
                    cart = _cartRepository.GetCartBySessionIdAsync(sessionId, CartStatus.PendingPayment).GetAwaiter().GetResult().Value;
                }
            }

            else
            {
                return new ApplicationResponse<CartResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid cart context."
                };
            }

            if (cart == null || cart.Id != cartId) return new ApplicationResponse<CartResponse>
            {
                IsSuccess = false,
                ErrorMessage = "Cart not found."
            };
            var item = cart.CartItems.FirstOrDefault(i => i.ProductId == productId);

            if (item == null)
            {
                return new ApplicationResponse<CartResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Item not found in cart."
                };
            }

            int quantityDifference = quantity - item.Quantity;

            if (quantityDifference == 0)
            {
                return new ApplicationResponse<CartResponse>
                {
                    IsSuccess = true,
                    Value = CartResponse.MapFromCart(cart)
                };
            }

            if (quantityDifference > 0)
            {
                if (! _inventoryService.IsInStockAsync(productId, quantityDifference).GetAwaiter().GetResult().Value)
                {
                    return new ApplicationResponse<CartResponse>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Product is not in stock."
                    };
                }

                if (! _inventoryService.ReserveStockAsync(
                    productId,
                    quantityDifference,
                    cartId,
                    "CartItem").GetAwaiter().GetResult().Value) return new ApplicationResponse<CartResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Failed to reserve stock."
                };
            }
            else
            {
                if (! _inventoryService.ReleaseReservedStockAsync(
                    productId,
                    Math.Abs(quantityDifference),
                    cartId,
                    "CartItem").GetAwaiter().GetResult().Value) return new ApplicationResponse<CartResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Failed to release reserved stock."
                };
            }

            var updatedQuantity = _cartRepository.UpdateItemQuantityAsync(userId, sessionId, productId, quantity).GetAwaiter().GetResult().Value;


            if (updatedQuantity == null && quantityDifference > 0)
            {
                await _inventoryService.ReleaseReservedStockAsync(
                    productId,
                    quantityDifference,
                    cartId,
                    "CartItem");
                return new ApplicationResponse<CartResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Failed to update item quantity."
                };
            }

            return new ApplicationResponse<CartResponse>
            {
                IsSuccess = true,
                Value = CartResponse.MapFromCart(updatedQuantity)
            };
        }


        public async Task<ApplicationResponse<bool>> RemoveItemAsync(int cartId, int? userId, string? sessionId, int productId)
        {
            try
            {
                _logger.LogDebug("Removing item from cart: CartId={CartId}, ProductId={ProductId}", cartId, productId);

                Cart cart = null;

                if (userId.HasValue)
                {
                    cart = _cartRepository.GetCartByUserIdAsync(userId, CartStatus.Active).GetAwaiter().GetResult().Value;
                    if (cart == null)
                    {
                        cart = _cartRepository.GetCartByUserIdAsync(userId, CartStatus.PendingPayment).GetAwaiter().GetResult().Value;
                    }
                }
                else if (!string.IsNullOrEmpty(sessionId))
                {
                    cart = _cartRepository.GetCartByIdAsync(cartId, CartStatus.Active).GetAwaiter().GetResult().Value;
                    if (cart == null)
                    {
                        cart = _cartRepository.GetCartByIdAsync(cartId, CartStatus.PendingPayment).GetAwaiter().GetResult().Value;
                    }
                }
                else
                {
                    _logger.LogWarning("No userId or sessionId provided for cart item removal");
                    return new ApplicationResponse<bool>
                    {
                        IsSuccess = false,
                        ErrorMessage = "No userId or sessionId provided for cart item removal"
                    };
                }

                if (cart == null)
                {
                    _logger.LogWarning("Cart not found for removal: CartId={CartId}", cartId);
                    return new ApplicationResponse<bool>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Cart not found for removal"
                    };
                }

                var item = cart.CartItems.FirstOrDefault(i => i.ProductId == productId);

                if (item == null)
                {
                    _logger.LogWarning("Cart item not found: CartId={CartId}, ProductId={ProductId}", cartId, productId);
                    return new ApplicationResponse<bool>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Cart item not found"
                    };
                }

                var quantityToRelease = item.Quantity;
                _logger.LogDebug("Releasing reserved stock: ProductId={ProductId}, Quantity={Quantity}", productId, quantityToRelease);

                await _inventoryService.ReleaseReservedStockAsync(
                    productId,
                    quantityToRelease,
                    cartId,
                    "CartItem");

                var removedItem = _cartRepository.RemoveItemAsync(userId, sessionId, productId).GetAwaiter().GetResult().Value;

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

                return new ApplicationResponse<bool>
                {
                    IsSuccess = true,
                    Value = removedItem
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item from cart: CartId={CartId}, ProductId={ProductId}", cartId, productId);
                return new ApplicationResponse<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = "Error occurred while removing item from cart"
                };
            }
        }


        public async Task<ApplicationResponse<bool>> ClearCartAsync(int cartId, int? userId, string? sessionId)
        {
            try
            {
                _logger.LogDebug("Clearing cart: CartId={CartId}", cartId);

                if (cartId <= 0)
                {
                    _logger.LogWarning("Invalid cart ID provided: {CartId}", cartId);
                    return new ApplicationResponse<bool>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Invalid cart ID provided"
                    };
                }

                
                Cart cart = _cartRepository.GetCartByIdAsync(cartId, CartStatus.Active).GetAwaiter().GetResult().Value;
                if (cart == null)
                {
                    cart = _cartRepository.GetCartByIdAsync(cartId, CartStatus.PendingPayment).GetAwaiter().GetResult().Value;
                }
                if (cart == null)
                {
                    cart = _cartRepository.GetCartByIdAsync(cartId, CartStatus.Converted).GetAwaiter().GetResult().Value;
                }

                if (cart == null)
                {
                    _logger.LogWarning("Cart not found for clearing: CartId={CartId}", cartId);
                    return new ApplicationResponse<bool>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Cart not found"
                    };
                }

                
                if (cart.UserId.HasValue ? cart.UserId != userId :
                    string.IsNullOrEmpty(sessionId) || cart.SessionId != sessionId)
                {
                    _logger.LogWarning("User {UserId} attempted to clear cart {CartId} owned by user {OwnerId}",
                        userId, cartId, cart.UserId);
                    return new ApplicationResponse<bool>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Unauthorized access to cart"
                    };
                }

                bool isConverted = cart.Status == CartStatus.Converted;

                if (cart.CartItems?.Any() == true && !isConverted)
                {
                    _logger.LogDebug("Releasing reserved stock for {ItemCount} items in cart {CartId}", cart.CartItems.Count, cartId);

                    foreach (var item in cart.CartItems)
                    {
                        try
                        {
                            var released = _inventoryService.ReleaseReservedStockAsync(
                                item.ProductId,
                                item.Quantity,
                                cartId,
                                "CartItem").GetAwaiter().GetResult().Value;
                            if (!released) return new ApplicationResponse<bool>
                            {
                                IsSuccess = false,
                                ErrorMessage = $"Failed to release stock for product {item.ProductId}"
                            };

                            _logger.LogDebug("Released stock for product {ProductId}, quantity {Quantity}",
                                item.ProductId, item.Quantity);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to release stock for product {ProductId} in cart {CartId}",
                                item.ProductId, cartId);
                            return new ApplicationResponse<bool>
                            {
                                IsSuccess = false,
                                ErrorMessage = $"Failed to release stock for product {item.ProductId}"
                            };
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

                return new ApplicationResponse<bool>
                {
                    IsSuccess = success
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart: CartId={CartId}", cartId);
                return new ApplicationResponse<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = "Error occurred while clearing the cart"
                };
            }
        }

        public async Task<ApplicationResponse<bool>> CompleteCartAsync(int cartId, int? userId)
        {
            var cart = _cartRepository.GetCartByIdAsync(cartId, CartStatus.PendingPayment).GetAwaiter().GetResult().Value;
            if (cart == null || !userId.HasValue || cart.UserId != userId) return new ApplicationResponse<bool>
            {
                IsSuccess = false,
                ErrorMessage = "Unauthorized access to cart"
            };
            var order = _orderRepository.GetOrderByCartIdAsync(cartId).GetAwaiter().GetResult().Value;
            if (order == null || !order.isPaid) return new ApplicationResponse<bool>
            {
                IsSuccess = false,
                ErrorMessage = "Order is not paid"
            };
            return ConvertCartAsync(cartId).GetAwaiter().GetResult();
        }

        public async Task<ApplicationResponse<bool>> ConvertCartAsync(int cartId)
        {
            try
            {
                _logger.LogInformation("Starting cart conversion for cart ID: {CartId}", cartId);

                var cart = _cartRepository.GetCartByIdAsync(cartId, CartStatus.PendingPayment).GetAwaiter().GetResult().Value;
                if (cart == null)
                {
                    _logger.LogWarning("Cart not found or not in PendingPayment status for conversion: {CartId}", cartId);
                    return new ApplicationResponse<bool>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Cart not found or not in PendingPayment status"
                    };
                }

                if (cart.CartItems == null || !cart.CartItems.Any())
                {
                    _logger.LogWarning("Cannot convert empty cart: {CartId}", cartId);
                    return new ApplicationResponse<bool>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Cannot convert empty cart"
                    };
                }

                cart.Status = CartStatus.Converted;
                cart.UpdatedAt = DateTime.UtcNow;

                var updatedCart = _cartRepository.UpdateCartAsync(cart).GetAwaiter().GetResult().Value;
                if (updatedCart == null)
                {
                    _logger.LogError("Failed to update cart status to Converted for cart {CartId}", cartId);
                    return new ApplicationResponse<bool>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Failed to update cart status"
                    };
                }

                _logger.LogInformation("Successfully converted cart {CartId} status to Converted", cartId);

                return new ApplicationResponse<bool>
                {
                    IsSuccess = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during cart conversion for cart {CartId}", cartId);
                return new ApplicationResponse<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = "Unexpected error occurred"
                };
            }
        }

        public async Task<ApplicationResponse<bool>> UpdateCartStatusAsync(int cartId, CartStatus status)
        {
            try
            {
                _logger.LogInformation("Updating cart {CartId} status to {Status}", cartId, status);

                var cart = _cartRepository.GetCartByIdAsync(cartId, CartStatus.Active).GetAwaiter().GetResult().Value;
                if (cart == null)
                {
                    cart = _cartRepository.GetCartByIdAsync(cartId, CartStatus.PendingPayment).GetAwaiter().GetResult().Value;
                }

                if (cart == null)
                {
                    _logger.LogWarning("Cart not found: {CartId}", cartId);
                    return new ApplicationResponse<bool>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Cart not found"
                    };
                }

                if (cart.Status == status)
                {
                    return new ApplicationResponse<bool>
                    {
                        IsSuccess = true,
                        Value = true
                    };
                }

                cart.Status = status;
                cart.UpdatedAt = DateTime.UtcNow;

                var updatedCart = _cartRepository.UpdateCartAsync(cart).GetAwaiter().GetResult().Value;
                if (updatedCart == null)
                {
                    _logger.LogError("Failed to update cart status for cart {CartId}", cartId);
                    return new ApplicationResponse<bool>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Failed to update cart status"
                    };
                }

                _logger.LogInformation("Successfully updated cart {CartId} status to {Status}", cartId, status);
                return new ApplicationResponse<bool>
                {
                    IsSuccess = true,
                    Value = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart status for cart {CartId}", cartId);
                return new ApplicationResponse<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = "Error updating cart status"
                };
            }
        }

        public async Task<ApplicationResponse<bool>> ReactivateCartAsync(int cartId, int orderId)
        {
            try
            {
                _logger.LogInformation("Reactivating cart {CartId} from order {OrderId}", cartId, orderId);

                var cart = _cartRepository.GetCartByIdAsync(cartId, CartStatus.PendingPayment).GetAwaiter().GetResult().Value;
                if (cart == null)
                {
                    _logger.LogWarning("Cart not found or not in PendingPayment status: {CartId}", cartId);
                    return new ApplicationResponse<bool>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Cart not found or not in PendingPayment status"
                    };
                }

                cart.Status = CartStatus.Active;
                cart.UpdatedAt = DateTime.UtcNow;

                var updatedCart = _cartRepository.UpdateCartAsync(cart).GetAwaiter().GetResult().Value;
                if (updatedCart == null)
                {
                    _logger.LogError("Failed to reactivate cart {CartId}", cartId);
                    return new ApplicationResponse<bool>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Failed to reactivate cart"
                    };
                }

                _logger.LogInformation("Successfully reactivated cart {CartId}", cartId);
                return new ApplicationResponse<bool>
                {
                    IsSuccess = true,
                    Value = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reactivating cart {CartId}", cartId);
                return new ApplicationResponse<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = "Error reactivating cart"
                };
            }
        }

        public async Task<ApplicationResponse<CartResponse>> RefreshCartAsync(int cartId)
        {
            return new ApplicationResponse<CartResponse>()
            {
                IsSuccess = true,
                Value = GetCartByIdAsync(cartId, CartStatus.Active).GetAwaiter().GetResult().Value
                ?? GetCartByIdAsync(cartId, CartStatus.PendingPayment).GetAwaiter().GetResult().Value
            };
        }


        public async Task<ApplicationResponse<CartItemResponse>> GetItemAsync(int cartId, int productId)
        {
            CartResponse? cart = null;

            cart = GetCartByIdAsync(cartId, CartStatus.Active).GetAwaiter().GetResult().Value;
            if (cart == null)
            {
                cart = GetCartByIdAsync(cartId, CartStatus.PendingPayment).GetAwaiter().GetResult().Value;
            }

            if (cart == null)
            {
                return new ApplicationResponse<CartItemResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Cart not found."
                };
            }

            var item = cart.CartItems.Where(i => i.ProductId == productId).FirstOrDefault();

            if (item == null)
            {
                return new ApplicationResponse<CartItemResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Item not found."
                };
            }

            return new ApplicationResponse<CartItemResponse>
            {
                IsSuccess = true,
                Value = item
            };
        }


        public async Task<ApplicationResponse<CartResponse>> ApplyCouponAsync(int cartId, int? userId, string couponCode)
        {
            try
            {

                var cart = _cartRepository.GetCartByIdAsync(cartId, CartStatus.Active).GetAwaiter().GetResult().Value;
                if (cart == null)
                {
                    cart = _cartRepository.GetCartByIdAsync(cartId, CartStatus.PendingPayment).GetAwaiter().GetResult().Value;
                }

                if (cart == null || !userId.HasValue || cart.UserId != userId)
                {
                    return new ApplicationResponse<CartResponse>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Cart not found or user is not the owner."
                    };
                }

                if (string.IsNullOrWhiteSpace(couponCode))
                {
                    return new ApplicationResponse<CartResponse>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Coupon code is invalid."
                    };
                }

                if (cart.CartCoupons.Any(cc => cc.Coupon.Code == couponCode))
                {
                    return new ApplicationResponse<CartResponse>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Coupon has already been applied."
                    };
                }

                if (!userId.HasValue || userId.Value <= 0)
                {
                    return new ApplicationResponse<CartResponse>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Invalid user."
                    };
                }

                var coupon = _couponService.GetByCodeAsync(couponCode).GetAwaiter().GetResult().Value;

                if (coupon == null || !coupon.IsActive || _userCouponService.IsCouponUsedByUser(userId.Value, couponCode).GetAwaiter().GetResult().Value)
                {
                    return new ApplicationResponse<CartResponse>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Invalid coupon."
                    };
                }

                var assignedCoupons = _userCouponService.GetCouponsByUserIdAsync(userId.Value).GetAwaiter().GetResult().Value;
                if (assignedCoupons == null || !assignedCoupons.Any(c => c.Id == coupon.Id))
                {
                    return new ApplicationResponse<CartResponse>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Coupon is not assigned to the user."
                    };
                }

                if (!cart.CartItems.Any())
                {
                    return new ApplicationResponse<CartResponse>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Cart is empty."
                    };
                }

                if (coupon.IsForNewUsersOnly && _orderRepository.UserHasPaidOrderAsync(userId.Value).GetAwaiter().GetResult().Value)
                {
                    return new ApplicationResponse<CartResponse>
                    {
                        IsSuccess = false,
                        ErrorMessage = "This coupon is only for new users."
                    };
                }

                decimal cartSubTotal = cart.SubTotal;

                if (coupon.MinimumOrderAmount > 0 && cartSubTotal < coupon.MinimumOrderAmount)
                {
                    return new ApplicationResponse<CartResponse>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Minimum order amount not met."
                    };
                }

                var discountAmount = ComputeDiscountAmount(coupon, cartSubTotal);
                if (discountAmount <= 0)
                {
                    return new ApplicationResponse<CartResponse>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Invalid discount amount."
                    };
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

                var updatedCart = _cartRepository.UpdateCartAsync(cart).GetAwaiter().GetResult().Value;
                if (updatedCart == null)
                {
                    return new ApplicationResponse<CartResponse>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Failed to update cart."
                    };
                }

                return new ApplicationResponse<CartResponse>
                {
                    IsSuccess = true,
                    Value = CartResponse.MapFromCart(updatedCart)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying coupon {CouponCode} to cart {CartId}", couponCode, cartId);
                return new ApplicationResponse<CartResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "An error occurred while applying the coupon."
                };
            }
        }

        public async Task<ApplicationResponse<CartResponse>> RemoveCouponAsync(int cartId, int? userId, string? sessionId, int couponId)
        {
            try
            {
                var cart = _cartRepository.GetCartByIdAsync(cartId, CartStatus.Active).GetAwaiter().GetResult().Value
                    ?? _cartRepository.GetCartByIdAsync(cartId, CartStatus.PendingPayment).GetAwaiter().GetResult().Value;
                if (cart == null || (userId.HasValue
                    ? cart.UserId != userId
                    : cart.UserId.HasValue || string.IsNullOrWhiteSpace(sessionId) || cart.SessionId != sessionId))
                {
                    return new ApplicationResponse<CartResponse>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Cart not found or not active."
                    };
                }

                var cartCoupon = cart.CartCoupons.FirstOrDefault(cc => cc.CouponId == couponId);
                if (cartCoupon == null)
                {
                    return new ApplicationResponse<CartResponse>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Coupon not found in cart."
                    };
                }

                cart.CartCoupons.Remove(cartCoupon);

                var updatedCart = _cartRepository.UpdateCartAsync(cart).GetAwaiter().GetResult().Value;
                if (updatedCart == null)
                {
                    return new ApplicationResponse<CartResponse>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Failed to remove coupon from cart."
                    };
                }

                return new ApplicationResponse<CartResponse>
                {
                    IsSuccess = true,
                    Value = CartResponse.MapFromCart(updatedCart)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing coupon {CouponId} from cart {CartId}", couponId, cartId);
                throw;
            }
        }

        public async Task<ApplicationResponse<bool>> HandleAbandonedCartAsync(int cartId)
        {
            try
            {
                _logger.LogInformation("Handling abandoned cart: {CartId}", cartId);

                var cart = _cartRepository.GetCartByIdAsync(cartId, CartStatus.Active).GetAwaiter().GetResult().Value;
                if (cart == null)
                {
                    _logger.LogWarning("Cart not found or not active: {CartId}", cartId);
                    return new ApplicationResponse<bool>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Cart not found or not active."
                    };
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

                var updatedCart = _cartRepository.UpdateCartAsync(cart).GetAwaiter().GetResult().Value;
                var success = updatedCart != null;

                if (success)
                {
                    _logger.LogInformation("Successfully handled abandoned cart {CartId}", cartId);
                }
                else
                {
                    _logger.LogError("Failed to update abandoned cart status: {CartId}", cartId);
                }

                return new ApplicationResponse<bool>
                {
                    IsSuccess = true,
                    Value = success
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling abandoned cart: {CartId}", cartId);
                return new ApplicationResponse<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = "Error handling abandoned cart."
                };
            }
        }

        public async Task<ApplicationResponse<int>> CleanupExpiredCartsAsync()
        {
            try
            {
                _logger.LogInformation("Starting cleanup of expired carts");

                var allCarts = _cartRepository.GetCartsAsync().GetAwaiter().GetResult().Value;
                var expiredCarts = allCarts.Where(c =>
                    c.Status == CartStatus.Active &&
                    c.UpdatedAt < DateTime.UtcNow.AddHours(-24))
                    .ToList();

                if (!expiredCarts.Any())
                {
                    _logger.LogInformation("No expired carts found for cleanup");
                    return new ApplicationResponse<int>
                    {
                        IsSuccess = true,
                        Value = 0
                    };
                }

                _logger.LogInformation("Found {ExpiredCartCount} expired carts for cleanup", expiredCarts.Count);

                int cleanedUpCount = 0;
                var cleanupTasks = expiredCarts.Select(async cart =>
                {
                    try
                    {
                        var success = HandleAbandonedCartAsync(cart.Id).GetAwaiter().GetResult().Value;
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

                return new ApplicationResponse<int>
                {
                    IsSuccess = true,
                    Value = cleanedUpCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during expired cart cleanup");
                return new ApplicationResponse<int>
                {
                    IsSuccess = false,
                    ErrorMessage = "Error during expired cart cleanup."
                };
            }
        }
    }
}
