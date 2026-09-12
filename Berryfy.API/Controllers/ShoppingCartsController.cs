using Berryfy.Application.Authorization.Attributes;
using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.ShoppingCartDtos;
using Berryfy.Application.Services.Interfaces.CouponServiceInterfaces;
using Berryfy.Application.Services.Interfaces.InventoryServiceInterfaces;
using Berryfy.Application.Services.Interfaces.OrderServiceInterfaces;
using Berryfy.Application.Services.Interfaces.OrchestrationServiceInterfaces;
using Berryfy.Application.Services.Interfaces.ShoppingCartServiceInterfaces;
using Berryfy.Domain.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Berryfy.API.Controllers
{
    [Route("api/shopping-carts")]
    [ApiController]
    public class ShoppingCartsController : BaseController, IAsyncActionFilter
    {
        private readonly IUserCouponService _userCouponService;
        private readonly ICartService _cartService;
        private readonly IInventoryService _inventoryService;
        private readonly IOrderService _orderService;
        private readonly ICheckoutOrchestrationService _checkoutOrchestrationService;

        [NonAction]
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var value = context.RouteData.Values["cartId"] ?? context.RouteData.Values["id"];
            if (value != null)
            {
                if (!int.TryParse(value.ToString(), out var cartId) || cartId <= 0)
                {
                    context.Result = BadRequest();
                    return;
                }

                CartDto? cart = null;
                foreach (var status in Enum.GetValues<CartStatus>())
                {
                    cart = await _cartService.GetCartByIdAsync(cartId, status);
                    if (cart != null) break;
                }

                var userId = GetCurrentUserId();
                var sessionId = GetSessionId();
                var ownsCart = cart != null && (cart.UserId.HasValue
                    ? userId.HasValue && cart.UserId == userId
                    : !string.IsNullOrEmpty(sessionId) && cart.SessionId == sessionId);
                if (!ownsCart)
                {
                    context.Result = NotFound();
                    return;
                }
            }

            await next();
        }

        public ShoppingCartsController(
            IUserCouponService userCouponService,
            ICartService cartService,
            IInventoryService inventoryService,
            IOrderService orderService,
            ICheckoutOrchestrationService checkoutOrchestrationService)
        {
            _userCouponService = userCouponService;
            _cartService = cartService;
            _inventoryService = inventoryService;
            _orderService = orderService;
            _checkoutOrchestrationService = checkoutOrchestrationService;
        }

        [HttpGet("user-id")]
        public async Task<ActionResult<ResponseDto<CartDto>>> GetCartByUserId([FromQuery] CartStatus? status = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized(new ResponseDto<CartDto>
                    {
                        IsSuccess = false,
                        StatusCode = 401,
                        StatusMessage = "User not authenticated"
                    });
                }

                CartDto cart = null;
                
                if (status.HasValue)
                {
                    // If specific status requested, get that cart
                    cart = await _cartService.GetCartByUserIdAsync(userId.Value, status);
                }
                else
                {
                    // If no status specified, check for PendingPayment first, then Active
                    cart = await _cartService.GetCartByUserIdAsync(userId.Value, CartStatus.PendingPayment);
                    if (cart == null)
                    {
                        cart = await _cartService.GetCartByUserIdAsync(userId.Value, CartStatus.Active);
                    }
                }
                
                if (cart == null)
                {
                    cart = await _cartService.CreateCartAsync(userId, null);
                    if (cart == null)
                    {
                        return StatusCode(500, new ResponseDto<CartDto>
                        {
                            IsSuccess = false,
                            StatusCode = 500,
                            StatusMessage = "Failed to create cart"
                        });
                    }
                }

                return Ok(new ResponseDto<CartDto>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = "Cart retrieved successfully",
                    Data = cart
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<CartDto>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "Error retrieving cart",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpGet("current")]
        public async Task<ActionResult<ResponseDto<CartDto>>> GetCurrentCart()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized(new ResponseDto<CartDto>
                    {
                        IsSuccess = false,
                        StatusCode = 401,
                        StatusMessage = "User not authenticated"
                    });
                }

                // Priority: PendingPayment > Active > Create new
                var cart = await _cartService.GetCartByUserIdAsync(userId.Value, CartStatus.PendingPayment);
                if (cart == null)
                {
                    cart = await _cartService.GetCartByUserIdAsync(userId.Value, CartStatus.Active);
                }
                
                if (cart == null)
                {
                    cart = await _cartService.CreateCartAsync(userId, null);
                    if (cart == null)
                    {
                        return StatusCode(500, new ResponseDto<CartDto>
                        {
                            IsSuccess = false,
                            StatusCode = 500,
                            StatusMessage = "Failed to create cart"
                        });
                    }
                }

                return Ok(new ResponseDto<CartDto>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = "Current cart retrieved successfully",
                    Data = cart
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<CartDto>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "Error retrieving current cart",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpGet("session-id")]
        public async Task<ActionResult<ResponseDto<CartDto>>> GetCartBySessionId()
        {
            try
            {
                var sessionId = GetSessionId();
                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    return BadRequest(new ResponseDto<CartDto>
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        StatusMessage = "Session ID is required"
                    });
                }

                var cart = await _cartService.GetCartBySessionIdAsync(sessionId, CartStatus.Active);
                if (cart == null)
                {
                    cart = await _cartService.CreateCartAsync(null, sessionId);
                    if (cart == null)
                    {
                        return StatusCode(500, new ResponseDto<CartDto>
                        {
                            IsSuccess = false,
                            StatusCode = 500,
                            StatusMessage = "Failed to create cart"
                        });
                    }
                }

                return Ok(new ResponseDto<CartDto>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = "Cart retrieved successfully",
                    Data = cart
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<CartDto>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "Error retrieving cart",
                    Errors = new List<string> { ex.Message }
                });
            }
        }


        [HttpGet("{id:int}")]
        public async Task<ActionResult<ResponseDto<CartDto>>> GetCartByCartId(int id, [FromQuery] CartStatus? status = null)
        {
            try
            {
                CartDto cart = null;
                
                if (status.HasValue)
                {
                    cart = await _cartService.GetCartByIdAsync(id, status.Value);
                }
                else
                {
                    cart = await _cartService.GetCartByIdAsync(id, CartStatus.Active);
                    if (cart == null)
                    {
                        cart = await _cartService.GetCartByIdAsync(id, CartStatus.PendingPayment);
                    }
                }
                
                if (cart == null)
                {
                    return NotFound(new ResponseDto<CartDto>
                    {
                        IsSuccess = false,
                        StatusCode = 404,
                        StatusMessage = "Cart not found"
                    });
                }

                return Ok(new ResponseDto<CartDto>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = "Cart retrieved successfully",
                    Data = cart
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<CartDto>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "Error retrieving cart",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPost("create")]
        public async Task<ActionResult<ResponseDto<CartDto>>> CreateCart()
        {
            try
            {
                var userId = GetCurrentUserId();
                var sessionId = GetSessionId();

                if (!userId.HasValue && string.IsNullOrWhiteSpace(sessionId))
                {
                    return BadRequest(new ResponseDto<CartDto>
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        StatusMessage = "Either userId or sessionId must be provided"
                    });
                }

                var cart = await _cartService.CreateCartAsync(userId, sessionId);
                if (cart == null)
                {
                    return StatusCode(500, new ResponseDto<CartDto>
                    {
                        IsSuccess = false,
                        StatusCode = 500,
                        StatusMessage = "Failed to create cart"
                    });
                }

                return CreatedAtAction(nameof(GetCartByCartId), new { id = cart.Id }, new ResponseDto<CartDto>
                {
                    IsSuccess = true,
                    StatusCode = 201,
                    StatusMessage = "Cart created successfully",
                    Data = cart
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<CartDto>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "Error creating cart",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPost("add-item")]
        public async Task<ActionResult<ResponseDto<CartDto>>> AddItemToCart([FromBody] AddItemRequest itemRequest)
        {
            try
            {
                if (itemRequest.Quantity <= 0)
                {
                    return BadRequest(new ResponseDto<CartDto>
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        StatusMessage = "Quantity must be greater than 0"
                    });
                }

                var isInStock = await _inventoryService.IsInStockAsync(itemRequest.ProductId, itemRequest.Quantity);
                if (!isInStock)
                {
                    return BadRequest(new ResponseDto<CartDto>
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        StatusMessage = "Insufficient stock for the requested quantity"
                    });
                }

                var updatedCart = await _cartService.AddItemAsync(
                    itemRequest.CartId, 
                    GetCurrentUserId(), 
                    GetSessionId(),
                    itemRequest.ProductId, 
                    itemRequest.Quantity);

                if (updatedCart == null)
                {
                    return StatusCode(500, new ResponseDto<CartDto>
                    {
                        IsSuccess = false,
                        StatusCode = 500,
                        StatusMessage = "Failed to add item to cart"
                    });
                }

                return Ok(new ResponseDto<CartDto>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = "Item added to cart successfully",
                    Data = updatedCart
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<CartDto>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "Error adding item to cart",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPut("{cartId}/update")]
        public async Task<ActionResult<ResponseDto<CartDto>>> UpdateItemQuantity(int cartId, [FromBody] AddItemRequest itemRequest)
        {
            try
            {
                if (itemRequest.Quantity < 0)
                {
                    return BadRequest(new ResponseDto<CartDto>
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        StatusMessage = "Quantity cannot be negative"
                    });
                }

                if (itemRequest.Quantity == 0)
                {
                    var removed = await _cartService.RemoveItemAsync(cartId, GetCurrentUserId(), GetSessionId(), itemRequest.ProductId);
                    if (!removed) return BadRequest();
                    return Ok(new ResponseDto<CartDto>
                    {
                        IsSuccess = true,
                        StatusCode = 200,
                        StatusMessage = "Item removed successfully"
                    });
                }

                var updatedCart = await _cartService.UpdateItemQuantityAsync(
                    cartId,
                    GetCurrentUserId(),
                    GetSessionId(),
                    itemRequest.ProductId, 
                    itemRequest.Quantity);

                if (updatedCart == null)
                {
                    return StatusCode(500, new ResponseDto<CartDto>
                    {
                        IsSuccess = false,
                        StatusCode = 500,
                        StatusMessage = "Failed to update item quantity"
                    });
                }

                return Ok(new ResponseDto<CartDto>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = "Item quantity updated successfully",
                    Data = updatedCart
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<CartDto>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "Error updating item quantity",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpDelete("{cartId}/remove/{productId}")]
        public async Task<ActionResult<ResponseDto<bool>>> RemoveItemFromCart(int cartId, int productId)
        {
            try
            {
                var success = await _cartService.RemoveItemAsync(cartId, GetCurrentUserId(), GetSessionId(), productId);
                if (!success)
                {
                    return NotFound(new ResponseDto<bool>
                    {
                        IsSuccess = false,
                        StatusCode = 404,
                        StatusMessage = "Item not found in cart"
                    });
                }

                return Ok(new ResponseDto<bool>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = "Item removed from cart successfully",
                    Data = true
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<bool>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "Error removing item from cart",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpDelete("{cartId}/clear")]
        public async Task<ActionResult<ResponseDto<bool>>> ClearCart(int cartId)
        {
            try
            {
                var success = await _cartService.ClearCartAsync(cartId, GetCurrentUserId(), GetSessionId());
                if (!success)
                {
                    return StatusCode(500, new ResponseDto<bool>
                    {
                        IsSuccess = false,
                        StatusCode = 500,
                        StatusMessage = "Failed to clear the cart"
                    });
                }

                return Ok(new ResponseDto<bool>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = "Cart cleared successfully",
                    Data = true
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<bool>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "Error clearing cart",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPost("{cartId}/complete")]
        [AdminAndAbove]
        public async Task<ActionResult<ResponseDto<bool>>> CompleteCart(int cartId)
        {
            try
            {
                var success = await _cartService.CompleteCartAsync(cartId, GetCurrentUserId());
                if (!success)
                {
                    return StatusCode(500, new ResponseDto<bool>
                    {
                        IsSuccess = false,
                        StatusCode = 500,
                        StatusMessage = "Failed to complete the cart"
                    });
                }

                return Ok(new ResponseDto<bool>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = "Cart completed successfully",
                    Data = true
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<bool>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "Error completing cart",
                    Errors = new List<string> { ex.Message }
                });
            }
        }



        [HttpGet("{cartId}/item/{productId}")]
        public async Task<ActionResult<ResponseDto<CartItemDto>>> GetCartItem(int cartId, int productId)
        {
            try
            {
                var item = await _cartService.GetItemAsync(cartId, productId);
                if (item == null)
                {
                    return NotFound(new ResponseDto<CartItemDto>
                    {
                        IsSuccess = false,
                        StatusCode = 404,
                        StatusMessage = "Item not found in cart"
                    });
                }

                return Ok(new ResponseDto<CartItemDto>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = "Cart item retrieved successfully",
                    Data = item
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<CartItemDto>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "Error retrieving cart item",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPost("{cartId}/apply-coupon")]
        [UserAndAbove]
        public async Task<ActionResult<ResponseDto<CartDto>>> ApplyCoupon(int cartId, [FromBody] ApplyCouponRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.CouponCode))
                {
                    return BadRequest(new ResponseDto<CartDto>
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        StatusMessage = "Coupon code is required"
                    });
                }

                var cart = await _cartService.ApplyCouponAsync(cartId, GetCurrentUserId(), request.CouponCode);
                if (cart == null)
                {
                    return BadRequest(new ResponseDto<CartDto>
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        StatusMessage = "Failed to apply coupon"
                    });
                }

                return Ok(new ResponseDto<CartDto>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = "Coupon applied successfully",
                    Data = cart
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<CartDto>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "Error applying coupon",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpDelete("{cartId}/remove-coupon/{couponId}")]
        public async Task<ActionResult<ResponseDto<CartDto>>> RemoveCoupon(int cartId, int couponId)
        {
            try
            {
                var cart = await _cartService.RemoveCouponAsync(cartId, GetCurrentUserId(), GetSessionId(), couponId);
                if (cart == null)
                {
                    return StatusCode(500, new ResponseDto<CartDto>
                    {
                        IsSuccess = false,
                        StatusCode = 500,
                        StatusMessage = "Failed to remove coupon"
                    });
                }

                return Ok(new ResponseDto<CartDto>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = "Coupon removed successfully",
                    Data = cart
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<CartDto>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "Error removing coupon",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPost("{cartId}/checkout")]
        [UserAndAbove]
        public async Task<ActionResult<ResponseDto<object>>> CheckoutCart(int cartId, [FromBody] CheckoutRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new ResponseDto<object>
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        StatusMessage = "Checkout data is required"
                    });
                }

                // Try Active status first, then PendingPayment
                var cart = await _cartService.GetCartByIdAsync(cartId, CartStatus.Active);
                if (cart == null)
                {
                    cart = await _cartService.GetCartByIdAsync(cartId, CartStatus.PendingPayment);
                }
                
                if (cart == null)
                {
                    return NotFound(new ResponseDto<object>
                    {
                        IsSuccess = false,
                        StatusCode = 404,
                        StatusMessage = "Cart not found or already completed"
                    });
                }

                if (cart.CartItems == null || cart.CartItems.Count == 0)
                {
                    return BadRequest(new ResponseDto<object>
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        StatusMessage = "Cart is empty"
                    });
                }

                var createOrderDto = new Application.Dtos.OrderDtos.CreateOrderDto
                {
                    UserId = GetCurrentUserId() ?? 0,
                    CartId = cartId,
                    CustomerEmail = request.CustomerEmail,
                    CustomerPhone = request.CustomerPhone,
                    ShippingName = request.ShippingName,
                    ShippingAddressLine1 = request.ShippingAddressLine1,
                    ShippingAddressLine2 = request.ShippingAddressLine2,
                    ShippingCity = request.ShippingCity,
                    ShippingState = request.ShippingState,
                    ShippingPostalCode = request.ShippingPostalCode,
                    ShippingCountry = request.ShippingCountry ?? "US",
                };

                var checkoutResult = await _checkoutOrchestrationService.ProcessCheckoutAsync(
                    cartId, 
                    createOrderDto, 
                    GetCurrentUserId());

                if (!checkoutResult.IsSuccess)
                {
                    return StatusCode(500, new ResponseDto<object>
                    {
                        IsSuccess = false,
                        StatusCode = 500,
                        StatusMessage = checkoutResult.ErrorMessage ?? "Failed to process checkout",
                        Errors = checkoutResult.Warnings
                    });
                }

                return CreatedAtAction("GetOrderById", "Orders", new { id = checkoutResult.Order.Id }, new ResponseDto<object>
                {
                    IsSuccess = true,
                    StatusCode = 201,
                    StatusMessage = "Order created successfully",
                    Data = new
                    {
                        id = checkoutResult.Order.Id,
                        orderNumber = checkoutResult.Order.ReferenceNumber,
                        total = checkoutResult.Order.Total,
                        status = checkoutResult.Order.Status.ToString(),
                        warnings = checkoutResult.Warnings
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<object>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "Error processing checkout",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPost("{cartId}/reactivate")]
        [UserAndAbove]
        public async Task<ActionResult<ResponseDto<CartDto>>> ReactivateCart(int cartId, [FromQuery] int orderId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized(new ResponseDto<CartDto>
                    {
                        IsSuccess = false,
                        StatusCode = 401,
                        StatusMessage = "User not authenticated"
                    });
                }

                var order = await _orderService.GetOrderByIdAsync(orderId);
                if (order == null)
                {
                    return NotFound(new ResponseDto<CartDto>
                    {
                        IsSuccess = false,
                        StatusCode = 404,
                        StatusMessage = "Order not found"
                    });
                }

                if (order.UserId != userId.Value)
                {
                    return Forbid();
                }

                if (order.CartId != cartId)
                {
                    return BadRequest(new ResponseDto<CartDto>
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        StatusMessage = "Cart ID does not match order's cart ID"
                    });
                }

                var success = await _cartService.ReactivateCartAsync(cartId, orderId);
                if (!success)
                {
                    return StatusCode(500, new ResponseDto<CartDto>
                    {
                        IsSuccess = false,
                        StatusCode = 500,
                        StatusMessage = "Failed to reactivate cart"
                    });
                }

                var cart = await _cartService.GetCartByIdAsync(cartId, CartStatus.Active);
                
                return Ok(new ResponseDto<CartDto>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = "Cart reactivated successfully. You can now modify your cart.",
                    Data = cart
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<CartDto>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "Error reactivating cart",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

    }

}