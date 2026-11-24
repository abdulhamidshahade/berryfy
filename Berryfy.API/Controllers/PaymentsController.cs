using Berryfy.Application.Authorization.Attributes;
using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.OrderDtos.Responses;
using Berryfy.Application.Dtos.PaymentDtos.Requests;
using Berryfy.Application.Dtos.PaymentDtos.Responses;
using Berryfy.Application.Services.Interfaces.OrderServiceInterfaces;
using Berryfy.Application.Services.Interfaces.PaymentServiceInterfaces;
using Berryfy.Application.Services.Interfaces.ShoppingCartServiceInterfaces;
using Berryfy.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Berryfy.API.Controllers
{
    [Route("api/payments")]
    [ApiController]
    public class PaymentsController : BaseController
    {
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;
        private readonly ICartService _cartService;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(
            IPaymentService paymentService, 
            IOrderService orderService,
            ICartService cartService,
            ILogger<PaymentsController> logger)
        {
            _paymentService = paymentService;
            _orderService = orderService;
            _cartService = cartService;
            _logger = logger;
        }


        [HttpPost("process")]
        [UserAndAbove]
        public async Task<IActionResult> ProcessPayment([FromBody] CreatePayment createPaymentDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            var sessionId = GetSessionId();

            if (!userId.HasValue) return Unauthorized();
            if (!createPaymentDto.OrderId.HasValue)
            {
                return BadRequest(new ApiResponse<PaymentResponseDto>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = "An order is required"
                });
            }

            var payableOrder = await _orderService.GetOrderByIdAsync(createPaymentDto.OrderId.Value);
            if (payableOrder == null || !payableOrder.IsSuccess || payableOrder.Value == null)
            {
                return NotFound(new ApiResponse<PaymentResponseDto>
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    StatusMessage = "Order not found"
                });
            }
            if (payableOrder.Value.UserId != userId.Value) return Forbid();
            if (payableOrder.Value.Status != OrderStatus.Pending || payableOrder.Value.IsPaid)
            {
                return Conflict(new ApiResponse<PaymentResponseDto>
                {
                    IsSuccess = false,
                    StatusCode = 409,
                    StatusMessage = "This order is not awaiting payment"
                });
            }
            if (createPaymentDto.Amount <= 0 || Math.Round(createPaymentDto.Amount, 2) != Math.Round(payableOrder.Value.Total, 2))
            {
                return BadRequest(new ApiResponse<PaymentResponseDto>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = "Payment amount must match the order total"
                });
            }
            if (!string.Equals(createPaymentDto.Currency, "USD", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new ApiResponse<PaymentResponseDto>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = "Orders must be paid in USD"
                });
            }

            var previousPayment = await _paymentService.GetPaymentByOrderIdAsync(payableOrder.Value.Id);
            if (previousPayment.IsSuccess && previousPayment.Value != null &&
                previousPayment.Value.Status is PaymentStatus.Completed or PaymentStatus.Processing)
            {
                return Conflict(new ApiResponse<PaymentResponseDto>
                {
                    IsSuccess = false,
                    StatusCode = 409,
                    StatusMessage = "Payment has already been submitted for this order"
                });
            }

            var result = await _paymentService.ProcessPaymentAsync(createPaymentDto, userId, sessionId);
            if (!result.IsSuccess || result.Value == null)
            {
                return BadRequest(new ApiResponse<PaymentResponseDto>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = result.ErrorMessage ?? "Failed to process payment"
                });
            }

            if (createPaymentDto.OrderId.HasValue)
            {
                var order = await _orderService.GetOrderByIdAsync(createPaymentDto.OrderId.Value);
                if (order != null && order.IsSuccess && order.Value != null)
                {
                    var mappedOrder = OrderResponse.MapToOrder(order.Value);

                    if (result.Value.Success)
                    {
                        if (order.Value.Status == OrderStatus.Pending)
                        {
                            var inventoryOk = await _orderService.DeductInventoryForPaidOrderAsync(order.Value.Id);
                            if (!inventoryOk.Value)
                            {
                                _logger.LogCritical(
                                    "Payment succeeded for order {OrderId} but inventory deduction failed. Manual reconciliation required.",
                                    order.Value.Id);
                                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<PaymentResponseDto>
                                {
                                    IsSuccess = false,
                                    StatusCode = 500,
                                    StatusMessage =
                                        "Payment was captured but inventory could not be finalized. Contact support with your order ID.",
                                    Data = result.Value
                                });
                            }
                        }

                        var paymentStatusUpdated = await _orderService.UpdateOrderPaymentStatusAsync(mappedOrder, PaymentStatus.Completed);
                        if (!paymentStatusUpdated.Value)
                            throw new InvalidOperationException("Could not finalize the order payment state");
                        mappedOrder.isPaid = true;
                        var orderStatusUpdated = await _orderService.UpdateOrderStatusAsync(mappedOrder, OrderStatus.Processing);
                        if (!orderStatusUpdated.Value)
                            throw new InvalidOperationException("Could not finalize the order status");

                        if (order.Value.CartId > 0)
                        {
                            var cart = await _cartService.GetCartByIdAsync(order.Value.CartId, CartStatus.PendingPayment);
                            if (cart != null && cart.IsSuccess && cart.Value != null)
                            {
                                var cartConverted = await _cartService.ConvertCartAsync(order.Value.CartId);
                                if (cartConverted.Value)
                                {
                                    await _cartService.ClearCartAsync(order.Value.CartId, userId, null);
                                }
                            }
                        }
                    }
                    else
                    {
                        await _orderService.UpdateOrderPaymentStatusAsync(mappedOrder, PaymentStatus.Failed);
                    }
                }
            }

            return Ok(new ApiResponse<PaymentResponseDto>
            {
                IsSuccess = true,
                StatusCode = 200,
                StatusMessage = result.Value.Message ?? result.SuccessMessage ?? "Payment processed",
                Data = result.Value
            });
        }


        [HttpGet("{id}")]
        [UserAndAbove]
        public async Task<IActionResult> GetPaymentById(int id)
        {
            var result = await _paymentService.GetPaymentByIdAsync(id);
            if (!result.IsSuccess || result.Value == null)
            {
                return NotFound(new ApiResponse<PaymentResponse>
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    StatusMessage = result.ErrorMessage ?? "Payment not found"
                });
            }

            if (!CanAccessUserResource(result.Value.UserId)) return Forbid();
            return Ok(new ApiResponse<PaymentResponse>
            {
                IsSuccess = true,
                StatusCode = 200,
                StatusMessage = "Payment retrieved successfully",
                Data = result.Value
            });
        }


        [HttpGet("transaction/{transactionId}")]
        [UserAndAbove]
        public async Task<IActionResult> GetPaymentByTransactionId(string transactionId)
        {
            var result = await _paymentService.GetPaymentByTransactionIdAsync(transactionId);
            if (!result.IsSuccess || result.Value == null)
            {
                return NotFound(new ApiResponse<PaymentResponse>
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    StatusMessage = result.ErrorMessage ?? "Payment not found"
                });
            }

            if (!CanAccessUserResource(result.Value.UserId)) return Forbid();
            return Ok(new ApiResponse<PaymentResponse>
            {
                IsSuccess = true,
                StatusCode = 200,
                StatusMessage = "Payment retrieved successfully",
                Data = result.Value
            });
        }


        [HttpGet("order/{orderId}")]
        [UserAndAbove]
        public async Task<IActionResult> GetPaymentByOrderId(int orderId)
        {
            var result = await _paymentService.GetPaymentByOrderIdAsync(orderId);
            if (!result.IsSuccess || result.Value == null)
            {
                return NotFound(new ApiResponse<PaymentResponse>
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    StatusMessage = result.ErrorMessage ?? "Payment not found"
                });
            }

            if (!CanAccessUserResource(result.Value.UserId)) return Forbid();
            return Ok(new ApiResponse<PaymentResponse>
            {
                IsSuccess = true,
                StatusCode = 200,
                StatusMessage = "Payment retrieved successfully",
                Data = result.Value
            });
        }


        [HttpGet]
        [AdminAndAbove]
        public async Task<IActionResult> GetAllPayments()
        {
            var result = await _paymentService.GetAllPaymentsAsync();
            if (!result.IsSuccess)
            {
                return BadRequest(new ApiResponse<IEnumerable<PaymentResponse>>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = result.ErrorMessage ?? "Failed to retrieve payments"
                });
            }

            return Ok(new ApiResponse<IEnumerable<PaymentResponse>>
            {
                IsSuccess = true,
                StatusCode = 200,
                StatusMessage = "Payments retrieved successfully",
                Data = result.Value
            });
        }


        [HttpGet("user/{userId}")]
        [UserAndAbove]
        public async Task<IActionResult> GetPaymentsByUserId(int userId)
        {
            if (!CanAccessUserResource(userId))
            {
                return Forbid();
            }

            var result = await _paymentService.GetPaymentsByUserIdAsync(userId);
            if (!result.IsSuccess)
            {
                return BadRequest(new ApiResponse<IEnumerable<PaymentResponse>>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = result.ErrorMessage ?? "Failed to retrieve user payments"
                });
            }

            return Ok(new ApiResponse<IEnumerable<PaymentResponse>>
            {
                IsSuccess = true,
                StatusCode = 200,
                StatusMessage = "User payments retrieved successfully",
                Data = result.Value
            });
        }


        [HttpGet("my-payments")]
        [UserAndAbove]
        public async Task<IActionResult> GetMyPayments(int page = 1, int pageSize = 10)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new ApiResponse<IEnumerable<PaymentResponse>>
                {
                    IsSuccess = false,
                    StatusCode = 401,
                    StatusMessage = "User not authenticated"
                });
            }

            var result = await _paymentService.GetPaginatedPaymentsByUserIdAsync(userId.Value, page, pageSize);
            if (!result.IsSuccess)
            {
                return BadRequest(new ApiResponse<IEnumerable<PaymentResponse>>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = result.ErrorMessage ?? "Failed to retrieve payments"
                });
            }

            return Ok(new ApiResponse<IEnumerable<PaymentResponse>>
            {
                IsSuccess = true,
                StatusCode = 200,
                StatusMessage = "Payments retrieved successfully",
                Data = result.Value
            });
        }


        [HttpGet("status/{status}")]
        [AdminAndAbove]
        public async Task<IActionResult> GetPaymentsByStatus(PaymentStatus status)
        {
            var result = await _paymentService.GetPaymentsByStatusAsync(status);
            if (!result.IsSuccess)
            {
                return BadRequest(new ApiResponse<IEnumerable<PaymentResponse>>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = result.ErrorMessage ?? "Failed to retrieve payments"
                });
            }

            return Ok(new ApiResponse<IEnumerable<PaymentResponse>>
            {
                IsSuccess = true,
                StatusCode = 200,
                StatusMessage = "Payments retrieved successfully",
                Data = result.Value
            });
        }


        [HttpGet("date-range")]
        [AdminAndAbove]
        public async Task<IActionResult> GetPaymentsByDateRange(DateTime startDate, DateTime endDate)
        {
            var result = await _paymentService.GetPaymentsByDateRangeAsync(startDate, endDate);
            if (!result.IsSuccess)
            {
                return BadRequest(new ApiResponse<IEnumerable<PaymentResponse>>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = result.ErrorMessage ?? "Failed to retrieve payments"
                });
            }

            return Ok(new ApiResponse<IEnumerable<PaymentResponse>>
            {
                IsSuccess = true,
                StatusCode = 200,
                StatusMessage = "Payments retrieved successfully",
                Data = result.Value
            });
        }


        [HttpGet("paginated")]
        [AdminAndAbove]
        public async Task<IActionResult> GetPaginatedPayments(int page = 1, int pageSize = 50)
        {
            var result = await _paymentService.GetPaginatedPaymentsAsync(page, pageSize);
            if (!result.IsSuccess)
            {
                return BadRequest(new ApiResponse<IEnumerable<PaymentResponse>>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = result.ErrorMessage ?? "Failed to retrieve payments"
                });
            }

            return Ok(new ApiResponse<IEnumerable<PaymentResponse>>
            {
                IsSuccess = true,
                StatusCode = 200,
                StatusMessage = "Payments retrieved successfully",
                Data = result.Value
            });
        }


        [HttpPut("{id}/status")]
        [AdminAndAbove]
        public async Task<IActionResult> UpdatePaymentStatus(int id, [FromBody] UpdatePaymentStatus updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _paymentService.UpdatePaymentStatusAsync(id, updateDto.Status, updateDto.Notes);
            if (!result.IsSuccess)
            {
                return BadRequest(new ApiResponse<PaymentResponseDto>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = result.ErrorMessage ?? "Failed to update payment status"
                });
            }

            return Ok(new ApiResponse<PaymentResponseDto>
            {
                IsSuccess = true,
                StatusCode = 200,
                StatusMessage = "Payment status updated successfully",
                Data = result.Value
            });
        }


        [HttpPost("{id}/refund")]
        [AdminAndAbove]
        public async Task<IActionResult> RefundPayment(int id, [FromBody] RefundPayment refundDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _paymentService.RefundPaymentAsync(id, refundDto.RefundAmount, refundDto.Reason);
            if (!result.IsSuccess)
            {
                return BadRequest(new ApiResponse<PaymentResponseDto>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = result.ErrorMessage ?? "Failed to refund payment"
                });
            }

            return Ok(new ApiResponse<PaymentResponseDto>
            {
                IsSuccess = true,
                StatusCode = 200,
                StatusMessage = "Payment refunded successfully",
                Data = result.Value
            });
        }


        [HttpDelete("{id}")]
        [AdminAndAbove]
        public async Task<IActionResult> DeletePayment(int id)
        {
            var result = await _paymentService.DeletePaymentAsync(id);
            if (!result.IsSuccess)
            {
                return BadRequest(new ApiResponse<bool>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = result.ErrorMessage ?? "Failed to delete payment",
                    Data = false
                });
            }

            return Ok(new ApiResponse<bool>
            {
                IsSuccess = true,
                StatusCode = 200,
                StatusMessage = "Payment deleted successfully",
                Data = result.Value
            });
        }


        [HttpGet("stats/count")]
        [AdminAndAbove]
        public async Task<IActionResult> GetTotalPaymentCount()
        {
            var result = await _paymentService.GetTotalPaymentCountAsync();
            if (!result.IsSuccess)
            {
                return BadRequest(new ApiResponse<int>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = result.ErrorMessage ?? "Failed to retrieve payment count"
                });
            }

            return Ok(new ApiResponse<int>
            {
                IsSuccess = true,
                StatusCode = 200,
                StatusMessage = "Total payment count retrieved successfully",
                Data = result.Value
            });
        }


        [HttpGet("stats/amount/total")]
        [AdminAndAbove]
        public async Task<IActionResult> GetTotalAmountByDateRange(DateTime startDate, DateTime endDate)
        {
            var result = await _paymentService.GetTotalAmountByDateRangeAsync(startDate, endDate);
            if (!result.IsSuccess)
            {
                return BadRequest(new ApiResponse<decimal>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = result.ErrorMessage ?? "Failed to retrieve total amount"
                });
            }

            return Ok(new ApiResponse<decimal>
            {
                IsSuccess = true,
                StatusCode = 200,
                StatusMessage = "Total payment amount retrieved successfully",
                Data = result.Value
            });
        }


        [HttpGet("search")]
        [AdminAndAbove]
        public async Task<IActionResult> SearchPayments(string searchTerm, int page = 1, int pageSize = 50)
        {
            var result = await _paymentService.SearchPaymentsAsync(searchTerm, page, pageSize);
            if (!result.IsSuccess)
            {
                return BadRequest(new ApiResponse<IEnumerable<PaymentResponse>>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = result.ErrorMessage ?? "Failed to search payments"
                });
            }

            return Ok(new ApiResponse<IEnumerable<PaymentResponse>>
            {
                IsSuccess = true,
                StatusCode = 200,
                StatusMessage = "Payments retrieved successfully",
                Data = result.Value
            });
        }


        [HttpPost("{transactionId}/verify")]
        [AdminAndAbove]
        public async Task<IActionResult> VerifyPaymentWithProvider(string transactionId)
        {
            var result = await _paymentService.VerifyPaymentWithProviderAsync(transactionId);
            if (!result.IsSuccess)
            {
                return BadRequest(new ApiResponse<PaymentResponseDto>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = result.ErrorMessage ?? "Failed to verify payment"
                });
            }

            return Ok(new ApiResponse<PaymentResponseDto>
            {
                IsSuccess = true,
                StatusCode = 200,
                StatusMessage = "Payment verified successfully",
                Data = result.Value
            });
        }
    }
}
