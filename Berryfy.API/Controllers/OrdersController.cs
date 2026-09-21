using AutoMapper;
using Berryfy.Application.Authorization.Attributes;
using Berryfy.Application.Dtos;
using Berryfy.Application.Services.Interfaces.OrderServiceInterfaces;
using Berryfy.Application.Services.Interfaces.OrchestrationServiceInterfaces;
using Berryfy.Application.Services.Interfaces.ShoppingCartServiceInterfaces;
using Berryfy.Domain.Constants;
using Berryfy.Domain.Entities.OrderEntities;
using Microsoft.AspNetCore.Mvc;
using Berryfy.Application.Dtos.OrderDtos.Requests;
using Berryfy.Application.Dtos.OrderDtos.Responses;

namespace Berryfy.API.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrdersController : BaseController
    {
        private readonly IOrderService _orderService;
        private readonly ICartService _cartService;

        public OrdersController(
            IOrderService orderService,
            ICartService cartService)
        {
            _orderService = orderService;
            _cartService = cartService;
        }


        [HttpPost]
        [UserAndAbove]
        public async Task<ActionResult<ResponseDto<Order>>> CreateOrder([FromBody] CreateOrder request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new ResponseDto<Order>
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        StatusMessage = "Invalid request data"
                    });
                }

                var userId = GetCurrentUserId();
                if (!userId.HasValue) return Unauthorized();
                var cart = await _cartService.GetCartByIdAsync(request.CartId, CartStatus.Active)
                    ?? await _cartService.GetCartByIdAsync(request.CartId, CartStatus.PendingPayment);
                if (cart == null) return NotFound();
                if (cart.UserId != userId.Value) return Forbid();

                request.UserId = userId.Value;
                var order = await _orderService.CreateOrderFromCartAsync(request.CartId, request);
                if (order == null)
                {
                    return StatusCode(500, new ResponseDto<Order>
                    {
                        IsSuccess = false,
                        StatusCode = 500,
                        StatusMessage = "Order could not be created"
                    });
                }

                return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, new ResponseDto<Order>
                {
                    Data = order,
                    IsSuccess = true,
                    StatusCode = 201,
                    StatusMessage = "Order created successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<Order>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "Error creating order",
                    Errors = new List<string> { ex.Message }
                });
            }
        }


        [HttpGet("{id}")]
        [UserAndAbove]
        public async Task<ActionResult<ResponseDto<OrderResponse>>> GetOrderById(int id)
        {
            try
            {
                var order = await _orderService.GetOrderByIdAsync(id);
                if (order == null)
                {
                    return NotFound(new ResponseDto<OrderResponse>
                    {
                        IsSuccess = false,
                        StatusCode = 404,
                        StatusMessage = "Order not found"
                    });
                }

                if (!CanAccessUserResource(order.UserId)) return Forbid();

                return Ok(new ResponseDto<OrderResponse>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = "Order retrieved successfully",
                    Data = order
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<OrderResponse>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "Error retrieving order",
                    Errors = new List<string> { ex.Message }
                });
            }
        }


        [HttpGet("user/{userId:int}")]
        [UserAndAbove]
        public async Task<ActionResult<ResponseDto<List<OrderResponse>>>> GetUserOrders(int userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                if (!CanAccessUserResource(userId))
                {
                    return Forbid();
                }

                var orders = await _orderService.GetUserOrdersAsync(userId, page, pageSize);

                return Ok(new ResponseDto<List<OrderResponse>>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = "User orders retrieved successfully",
                    Data = orders
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<List<OrderResponse>>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "Error retrieving user orders",
                    Errors = new List<string> { ex.Message }
                });
            }
        }


        [HttpPut("{orderId}/cancel")]
        [AdminAndAbove]
        public async Task<ActionResult<ResponseDto<CancellationResult>>> CancelOrder(int orderId, [FromBody] CancelOrderRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.Reason))
                {
                    return BadRequest(new ResponseDto<CancellationResult>
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        StatusMessage = "Cancellation reason is required"
                    });
                }

                var result = await _orderService.CancelOrderAsync(orderId, request.Reason);
                if (!result)
                {
                    return StatusCode(500, new ResponseDto<CancellationResult>
                    {
                        IsSuccess = false,
                        StatusCode = 500,
                        StatusMessage = "Could not cancel the order"
                    });
                }

                return Ok(new ResponseDto<CancellationResult>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = "Order cancelled successfully",
                    Data = new CancellationResult
                    {
                        IsSuccess = true,
                        InventoryRestored = true,
                        CouponsReverted = true
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<CancellationResult>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "Error cancelling order",
                    Errors = new List<string> { ex.Message }
                });
            }
        }


        [HttpPut("{orderId}/refund")]
        [AdminAndAbove]
        public async Task<ActionResult<ResponseDto<RefundResult>>> RefundOrder(int orderId, [FromBody] RefundOrderRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.Reason))
                {
                    return BadRequest(new ResponseDto<RefundResult>
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        StatusMessage = "Refund reason is required"
                    });
                }

                var result = await _orderService.RefundOrderAsync(orderId, request.Reason);
                if (!result)
                {
                    return StatusCode(500, new ResponseDto<RefundResult>
                    {
                        IsSuccess = false,
                        StatusCode = 500,
                        StatusMessage = "Could not refund the order"
                    });
                }

                return Ok(new ResponseDto<RefundResult>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = "Order refunded successfully",
                    Data = new RefundResult
                    {
                        IsSuccess = true,
                        PaymentRefunded = true,
                        InventoryRestored = true,
                        CouponsReverted = true
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<RefundResult>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "Error refunding order",
                    Errors = new List<string> { ex.Message }
                });
            }
        }


        [HttpGet("calculate-totals")]
        [UserAndAbove]
        public async Task<ActionResult<ResponseDto<OrderTotal>>> CalculateOrderTotals([FromQuery] int cartId)
        {
            try
            {
                var cart = await _cartService.GetCartByIdAsync(cartId, CartStatus.Active)
                    ?? await _cartService.GetCartByIdAsync(cartId, CartStatus.PendingPayment);
                if (cart == null) return NotFound();
                if (!CanAccessUserResource(cart.UserId)) return Forbid();

                var totals = await _orderService.CalculateOrderTotalsAsync(cartId);
                if (totals == null)
                {
                    return NotFound(new ResponseDto<OrderTotal>
                    {
                        IsSuccess = false,
                        StatusCode = 404,
                        StatusMessage = "Cart not found"
                    });
                }

                return Ok(new ResponseDto<OrderTotal>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = "Order totals calculated successfully",
                    Data = totals
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<OrderTotal>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "Error calculating order totals",
                    Errors = new List<string> { ex.Message }
                });
            }
        }


        [HttpPut("{orderId}/update-status")]
        [AdminAndAbove]
        public async Task<ActionResult<ResponseDto<OrderResponse>>> UpdateOrderStatus(int orderId, [FromBody] UpdateOrderStatusRequest request)
        {
            try
            {
                var order = await _orderService.GetOrderByIdAsync(orderId);
                if (order == null)
                {
                    return NotFound(new ResponseDto<OrderResponse>
                    {
                        IsSuccess = false,
                        StatusCode = 404,
                        StatusMessage = "Order not found"
                    });
                }

                var mappedOrder = OrderResponse.MapToOrder(order);
                var result = await _orderService.UpdateOrderStatusAsync(mappedOrder, request.NewStatus);
                if (!result)
                {
                    return Conflict(new ResponseDto<OrderResponse>
                    {
                        IsSuccess = false,
                        StatusCode = 409,
                        StatusMessage = "Invalid order transition. Use cancellation or refund for returns; payment is required before fulfillment."
                    });
                }

                return Ok(new ResponseDto<OrderResponse>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = "Order status updated successfully",
                    Data = await _orderService.GetOrderByIdAsync(orderId)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<OrderResponse>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "Error updating order status",
                    Errors = new List<string> { ex.Message }
                });
            }
        }


        [HttpGet("reference/{referenceNumber}")]
        [AdminAndAbove]
        public async Task<ActionResult<ResponseDto<OrderResponse>>> GetOrderByReference(string referenceNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(referenceNumber))
                {
                    return BadRequest(new ResponseDto<OrderResponse>
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        StatusMessage = "Reference number is required"
                    });
                }

                var order = await _orderService.GetOrderByReferenceNumberAsync(referenceNumber);
                if (order == null)
                {
                    return NotFound(new ResponseDto<OrderResponse>
                    {
                        IsSuccess = false,
                        StatusCode = 404,
                        StatusMessage = "Order not found"
                    });
                }

                return Ok(new ResponseDto<OrderResponse>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = "Order retrieved successfully",
                    Data = OrderResponse.MapFromOrder(order)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<OrderResponse>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "Error retrieving order",
                    Errors = new List<string> { ex.Message }
                });
            }
        }


        [HttpGet("status/{status}")]
        [AdminAndAbove]
        public async Task<ActionResult<ResponseDto<List<OrderResponse>>>> GetOrdersByStatus(OrderStatus status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var orders = await _orderService.GetOrdersByStatusAsync(status, page, pageSize);

                return Ok(new ResponseDto<List<OrderResponse>>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = "Orders retrieved successfully",
                    Data = OrderResponse.MapFromOrder(orders)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<List<OrderResponse>>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "Error retrieving orders",
                    Errors = new List<string> { ex.Message }
                });
            }
        }


        [HttpGet("admin/all")]
        [AdminAndAbove]
        public async Task<ActionResult<ResponseDto<List<OrderResponse>>>> GetAllOrdersForAdmin([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var orders = await _orderService.GetAllOrdersAsync(page, pageSize);

                return Ok(new ResponseDto<List<OrderResponse>>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = "All orders retrieved successfully",
                    Data = orders
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<List<OrderResponse>>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "Error retrieving all orders",
                    Errors = new List<string> { ex.Message }
                });
            }
        }


        [HttpPut("{orderId}/process")]
        [AdminAndAbove]
        public async Task<ActionResult<ResponseDto<bool>>> ProcessOrder(int orderId)
        {
            try
            {
                var result = await _orderService.ProcessOrderAsync(orderId);
                if (!result)
                {
                    return StatusCode(500, new ResponseDto<bool>
                    {
                        IsSuccess = false,
                        StatusCode = 500,
                        StatusMessage = "Could not process the order"
                    });
                }

                return Ok(new ResponseDto<bool>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = "Order processed successfully",
                    Data = true
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<bool>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "Error processing order",
                    Errors = new List<string> { ex.Message }
                });
            }
        }


        [HttpPut("{orderId}/sync-with-cart")]
        [UserAndAbove]
        public async Task<ActionResult<ResponseDto<OrderResponse>>> SyncOrderWithCart(int orderId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var order = await _orderService.GetOrderByIdAsync(orderId);
                
                if (order == null)
                {
                    return NotFound(new ResponseDto<OrderResponse>
                    {
                        IsSuccess = false,
                        StatusCode = 404,
                        StatusMessage = "Order not found"
                    });
                }

                if (!CanAccessUserResource(order.UserId))
                {
                    return Forbid();
                }

                if (order.CartId <= 0)
                {
                    return BadRequest(new ResponseDto<OrderResponse>
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        StatusMessage = "Order does not have an associated cart"
                    });
                }

                var result = await _orderService.SyncOrderWithCartAsync(orderId, order.CartId);
                if (!result)
                {
                    return StatusCode(500, new ResponseDto<OrderResponse>
                    {
                        IsSuccess = false,
                        StatusCode = 500,
                        StatusMessage = "Failed to sync order with cart"
                    });
                }

                var updatedOrder = await _orderService.GetOrderByIdAsync(orderId);
                return Ok(new ResponseDto<OrderResponse>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = "Order synced with cart successfully",
                    Data = updatedOrder
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<OrderResponse>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "Error syncing order with cart",
                    Errors = new List<string> { ex.Message }
                });
            }
        }
    }

}
