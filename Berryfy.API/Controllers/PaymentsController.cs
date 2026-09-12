using AutoMapper;
using Berryfy.Application.Authorization.Attributes;
using Berryfy.Application.Dtos.PaymentDtos;
using Berryfy.Application.Services.Interfaces.OrderServiceInterfaces;
using Berryfy.Application.Services.Interfaces.PaymentServiceInterfaces;
using Berryfy.Application.Services.Interfaces.ShoppingCartServiceInterfaces;
using Berryfy.Domain.Constants;
using Berryfy.Domain.Entities.OrderEntities;
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
        private readonly IMapper _mapper;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(
            IPaymentService paymentService, 
            IOrderService orderService,
            ICartService cartService,
            IMapper mapper,
            ILogger<PaymentsController> logger)
        {
            _paymentService = paymentService;
            _orderService = orderService;
            _cartService = cartService;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpPost("process")]
        [UserAndAbove]
        public async Task<IActionResult> ProcessPayment([FromBody] CreatePaymentDto createPaymentDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = GetCurrentUserId();
                var sessionId = GetSessionId();

                if (!userId.HasValue) return Unauthorized();
                if (!createPaymentDto.OrderId.HasValue) return BadRequest(new { StatusMessage = "An order is required" });
                var payableOrder = await _orderService.GetOrderByIdAsync(createPaymentDto.OrderId.Value);
                if (payableOrder == null) return NotFound();
                if (payableOrder.UserId != userId.Value) return Forbid();
                if (payableOrder.Status != OrderStatus.Pending || payableOrder.IsPaid)
                    return Conflict(new { StatusMessage = "This order is not awaiting payment" });
                if (createPaymentDto.Amount <= 0 || createPaymentDto.Amount != payableOrder.Total)
                    return BadRequest(new { StatusMessage = "Payment amount must match the order total" });
                if (!string.Equals(createPaymentDto.Currency, "USD", StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { StatusMessage = "Orders must be paid in USD" });

                var previousPayment = await _paymentService.GetPaymentByOrderIdAsync(payableOrder.Id);
                if (previousPayment.IsSuccess && previousPayment.Data?.Status is PaymentStatus.Completed or PaymentStatus.Processing)
                    return Conflict(new { StatusMessage = "Payment has already been submitted for this order" });

                var result = await _paymentService.ProcessPaymentAsync(createPaymentDto, userId, sessionId);
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                if (createPaymentDto.OrderId.HasValue)
                {
                    var order = await _orderService.GetOrderByIdAsync(createPaymentDto.OrderId.Value);
                    if (order != null)
                    {
                        var mappedOrder = _mapper.Map<Order>(order);
                        
                        if (result.Data?.Success == true)
                        {
                            if (order.Status == OrderStatus.Pending)
                            {
                                var inventoryOk = await _orderService.DeductInventoryForPaidOrderAsync(order.Id);
                                if (!inventoryOk)
                                {
                                    _logger.LogCritical(
                                        "Payment succeeded for order {OrderId} but inventory deduction failed. Manual reconciliation required.",
                                        order.Id);
                                    return StatusCode(StatusCodes.Status500InternalServerError, new
                                    {
                                        IsSuccess = false,
                                        StatusCode = 500,
                                        StatusMessage =
                                            "Payment was captured but inventory could not be finalized. Contact support with your order ID.",
                                        OrderId = order.Id,
                                        Data = result.Data
                                    });
                                }
                            }

                            await _orderService.UpdateOrderStatusAsync(mappedOrder, OrderStatus.Processing);
                            await _orderService.UpdateOrderPaymentStatusAsync(mappedOrder, PaymentStatus.Completed);

                            if (order.CartId > 0)
                            {
                                var cart = await _cartService.GetCartByIdAsync(order.CartId, CartStatus.PendingPayment);
                                if (cart != null)
                                {
                                    var cartConverted = await _cartService.ConvertCartAsync(order.CartId);
                                    if (cartConverted)
                                    {
                                        await _cartService.ClearCartAsync(order.CartId, userId, null);
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

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { IsSuccess = false, StatusCode = 500, StatusMessage = "An error occurred while processing payment", Errors = new[] { ex.Message } });
            }
        }

        [HttpGet("{id}")]
        [UserAndAbove]
        public async Task<IActionResult> GetPaymentById(int id)
        {
            try
            {
                var result = await _paymentService.GetPaymentByIdAsync(id);
                if (!result.IsSuccess)
                {
                    return NotFound(result);
                }

                if (!CanAccessUserResource(result.Data?.UserId)) return Forbid();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { IsSuccess = false, StatusCode = 500, StatusMessage = "An error occurred while retrieving payment", Errors = new[] { ex.Message } });
            }
        }

        [HttpGet("transaction/{transactionId}")]
        [UserAndAbove]
        public async Task<IActionResult> GetPaymentByTransactionId(string transactionId)
        {
            try
            {
                var result = await _paymentService.GetPaymentByTransactionIdAsync(transactionId);
                if (!result.IsSuccess)
                {
                    return NotFound(result);
                }

                if (!CanAccessUserResource(result.Data?.UserId)) return Forbid();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { IsSuccess = false, StatusCode = 500, StatusMessage = "An error occurred while retrieving payment", Errors = new[] { ex.Message } });
            }
        }

        [HttpGet("order/{orderId}")]
        [UserAndAbove]
        public async Task<IActionResult> GetPaymentByOrderId(int orderId)
        {
            try
            {
                var result = await _paymentService.GetPaymentByOrderIdAsync(orderId);
                if (!result.IsSuccess)
                {
                    return NotFound(result);
                }

                if (!CanAccessUserResource(result.Data?.UserId)) return Forbid();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { IsSuccess = false, StatusCode = 500, StatusMessage = "An error occurred while retrieving payment", Errors = new[] { ex.Message } });
            }
        }

        [HttpGet]
        [AdminAndAbove]
        public async Task<IActionResult> GetAllPayments()
        {
            try
            {
                var result = await _paymentService.GetAllPaymentsAsync();
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { IsSuccess = false, StatusCode = 500, StatusMessage = "An error occurred while retrieving payments", Errors = new[] { ex.Message } });
            }
        }

        [HttpGet("user/{userId}")]
        [UserAndAbove]
        public async Task<IActionResult> GetPaymentsByUserId(int userId)
        {
            try
            {
                if (!CanAccessUserResource(userId))
                {
                    return Forbid();
                }

                var result = await _paymentService.GetPaymentsByUserIdAsync(userId);
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { IsSuccess = false, StatusCode = 500, StatusMessage = "An error occurred while retrieving user payments", Errors = new[] { ex.Message } });
            }
        }

        [HttpGet("my-payments")]
        [UserAndAbove]
        public async Task<IActionResult> GetMyPayments(int page = 1, int pageSize = 10)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { IsSuccess = false, StatusCode = 401, StatusMessage = "User not authenticated" });
                }

                var result = await _paymentService.GetPaginatedPaymentsByUserIdAsync(userId.Value, page, pageSize);
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { IsSuccess = false, StatusCode = 500, StatusMessage = "An error occurred while retrieving your payments", Errors = new[] { ex.Message } });
            }
        }

        [HttpGet("status/{status}")]
        [AdminAndAbove]
        public async Task<IActionResult> GetPaymentsByStatus(PaymentStatus status)
        {
            try
            {
                var result = await _paymentService.GetPaymentsByStatusAsync(status);
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { IsSuccess = false, StatusCode = 500, StatusMessage = "An error occurred while retrieving payments by status", Errors = new[] { ex.Message } });
            }
        }

        [HttpGet("date-range")]
        [AdminAndAbove]
        public async Task<IActionResult> GetPaymentsByDateRange(DateTime startDate, DateTime endDate)
        {
            try
            {
                var result = await _paymentService.GetPaymentsByDateRangeAsync(startDate, endDate);
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { IsSuccess = false, StatusCode = 500, StatusMessage = "An error occurred while retrieving payments by date range", Errors = new[] { ex.Message } });
            }
        }

        [HttpGet("paginated")]
        [AdminAndAbove]
        public async Task<IActionResult> GetPaginatedPayments(int page = 1, int pageSize = 50)
        {
            try
            {
                var result = await _paymentService.GetPaginatedPaymentsAsync(page, pageSize);
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { IsSuccess = false, StatusCode = 500, StatusMessage = "An error occurred while retrieving paginated payments", Errors = new[] { ex.Message } });
            }
        }

        [HttpPut("{id}/status")]
        [AdminAndAbove]
        public async Task<IActionResult> UpdatePaymentStatus(int id, [FromBody] UpdatePaymentStatusDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _paymentService.UpdatePaymentStatusAsync(id, updateDto.Status, updateDto.Notes);
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { IsSuccess = false, StatusCode = 500, StatusMessage = "An error occurred while updating payment status", Errors = new[] { ex.Message } });
            }
        }

        [HttpPost("{id}/refund")]
        [AdminAndAbove]
        public async Task<IActionResult> RefundPayment(int id, [FromBody] RefundPaymentDto refundDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _paymentService.RefundPaymentAsync(id, refundDto.RefundAmount, refundDto.Reason);
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { IsSuccess = false, StatusCode = 500, StatusMessage = "An error occurred while processing refund", Errors = new[] { ex.Message } });
            }
        }

        [HttpDelete("{id}")]
        [AdminAndAbove]
        public async Task<IActionResult> DeletePayment(int id)
        {
            try
            {
                var result = await _paymentService.DeletePaymentAsync(id);
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { IsSuccess = false, StatusCode = 500, StatusMessage = "An error occurred while deleting payment", Errors = new[] { ex.Message } });
            }
        }

        [HttpGet("stats/count")]
        [AdminAndAbove]
        public async Task<IActionResult> GetTotalPaymentCount()
        {
            try
            {
                var result = await _paymentService.GetTotalPaymentCountAsync();
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { IsSuccess = false, StatusCode = 500, StatusMessage = "An error occurred while retrieving payment count", Errors = new[] { ex.Message } });
            }
        }

        [HttpGet("stats/amount/total")]
        [AdminAndAbove]
        public async Task<IActionResult> GetTotalAmountByDateRange(DateTime startDate, DateTime endDate)
        {
            try
            {
                var result = await _paymentService.GetTotalAmountByDateRangeAsync(startDate, endDate);
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { IsSuccess = false, StatusCode = 500, StatusMessage = "An error occurred while retrieving total amount", Errors = new[] { ex.Message } });
            }
        }

        [HttpGet("search")]
        [AdminAndAbove]
        public async Task<IActionResult> SearchPayments(string searchTerm, int page = 1, int pageSize = 50)
        {
            try
            {
                var result = await _paymentService.SearchPaymentsAsync(searchTerm, page, pageSize);
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { IsSuccess = false, StatusCode = 500, StatusMessage = "An error occurred while searching payments", Errors = new[] { ex.Message } });
            }
        }

        [HttpPost("{transactionId}/verify")]
        [AdminAndAbove]
        public async Task<IActionResult> VerifyPaymentWithProvider(string transactionId)
        {
            try
            {
                var result = await _paymentService.VerifyPaymentWithProviderAsync(transactionId);
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { IsSuccess = false, StatusCode = 500, StatusMessage = "An error occurred while verifying payment", Errors = new[] { ex.Message } });
            }
        }
    }
}
