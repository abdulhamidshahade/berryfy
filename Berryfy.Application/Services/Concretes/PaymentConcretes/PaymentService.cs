using AutoMapper;
using Berryfy.Application.Dtos;
using Berryfy.Application.Services.Interfaces.PaymentServiceInterfaces;
using Berryfy.Domain.Constants;
using Berryfy.Domain.Entities.PaymentEntities;
using Berryfy.Domain.Repositories.PaymentInterfaces;
using Microsoft.Extensions.Logging;
using Berryfy.Application.Dtos.PaymentDtos.Requests;
using Berryfy.Application.Dtos.PaymentDtos.Responses;

namespace Berryfy.Application.Services.Concretes.PaymentConcretes
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            IPaymentRepository paymentRepository,
            ILogger<PaymentService> logger)
        {
            _paymentRepository = paymentRepository;
            _logger = logger;
        }

        public async Task<ApplicationResponse<PaymentResponseDto>> ProcessPaymentAsync(CreatePayment createPaymentDto, int? userId, string? sessionId)
        {
            try
            {
                var transactionId = GenerateTransactionIdAsync().Value;
                var processingFee = CalculateProcessingFee(createPaymentDto.Amount, createPaymentDto.Provider).Value;
                var netAmount = createPaymentDto.Amount - processingFee;

                var payment = new Payment
                {
                    UserId = userId,
                    OrderId = createPaymentDto.OrderId,
                    TransactionId = transactionId,
                    Status = PaymentStatus.Processing,
                    Method = createPaymentDto.Method,
                    Provider = createPaymentDto.Provider,
                    Amount = createPaymentDto.Amount,
                    Currency = createPaymentDto.Currency,
                    ProviderPaymentMethodId = createPaymentDto.ProviderPaymentMethodId,
                    CardLast4 = createPaymentDto.CardLast4,
                    CardBrand = createPaymentDto.CardBrand,
                    PayerEmail = createPaymentDto.PayerEmail,
                    PayerName = createPaymentDto.PayerName,
                    BillingAddress1 = createPaymentDto.BillingAddress1,
                    BillingAddress2 = createPaymentDto.BillingAddress2,
                    BillingCity = createPaymentDto.BillingCity,
                    BillingState = createPaymentDto.BillingState,
                    BillingPostalCode = createPaymentDto.BillingPostalCode,
                    BillingCountry = createPaymentDto.BillingCountry,
                    ProcessingFee = processingFee,
                    NetAmount = netAmount,
                    ProcessedAt = DateTime.UtcNow,
                    Metadata = createPaymentDto.Metadata,
                    Notes = createPaymentDto.Notes
                };

                var providerResult = ProcessWithPaymentProvider(payment).GetAwaiter().GetResult().Value;

                if (providerResult.Success)
                {
                    payment.Status = PaymentStatus.Completed;
                    payment.CompletedAt = DateTime.UtcNow;
                    payment.ProviderTransactionId = providerResult.ProviderTransactionId;
                }
                else
                {
                    payment.Status = PaymentStatus.Failed;
                    payment.FailedAt = DateTime.UtcNow;
                    payment.ErrorMessage = providerResult.ErrorMessage;
                    payment.FailureReason = providerResult.FailureReason;
                }

                var createdPayment = _paymentRepository.CreateAsync(payment).GetAwaiter().GetResult().Value;
                var paymentDto = PaymentResponse.MapFromPayment(createdPayment);

                var response = new PaymentResponseDto
                {
                    Success = providerResult.Success,
                    Message = providerResult.Success ? "Payment processed successfully" : "Payment failed",
                    Payment = paymentDto,
                    TransactionId = transactionId,
                    Status = payment.Status,
                    RedirectUrl = providerResult.Success ? "/payment/success" : "/payment/failed"
                };

                return new ApplicationResponse<PaymentResponseDto>
                {
                    IsSuccess = true,
                    SuccessMessage = "Payment processing completed",
                    Value = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment");
                return new ApplicationResponse<PaymentResponseDto>
                {
                    IsSuccess = false,
                    ErrorMessage = "An error occurred while processing payment"
                };
            }
        }

        public async Task<ApplicationResponse<PaymentResponse>> GetPaymentByIdAsync(int id)
        {
            try
            {
                var payment = _paymentRepository.GetByIdAsync(id).GetAwaiter().GetResult().Value;
                if (payment == null)
                {
                    return new ApplicationResponse<PaymentResponse>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Payment not found"
                    };
                }

                var paymentDto = PaymentResponse.MapFromPayment(payment);
                return new ApplicationResponse<PaymentResponse>
                {
                    IsSuccess = true,
                    Value = paymentDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment by ID: {PaymentId}", id);
                return new ApplicationResponse<PaymentResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "An error occurred while retrieving payment"
                };
            }
        }

        public async Task<ApplicationResponse<PaymentResponse>> GetPaymentByTransactionIdAsync(string transactionId)
        {
            try
            {
                var payment = _paymentRepository.GetByTransactionIdAsync(transactionId).GetAwaiter().GetResult().Value;
                if (payment == null)
                {
                    return new ApplicationResponse<PaymentResponse>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Payment not found"
                    };
                }

                var paymentDto = PaymentResponse.MapFromPayment(payment);
                return new ApplicationResponse<PaymentResponse>
                {
                    IsSuccess = true,
                    Value = paymentDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment by transaction ID: {TransactionId}", transactionId);
                return new ApplicationResponse<PaymentResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "An error occurred while retrieving payment"
                };
            }
        }

        public async Task<ApplicationResponse<PaymentResponse>> GetPaymentByOrderIdAsync(int orderId)
        {
            try
            {
                var payment = _paymentRepository.GetByOrderIdAsync(orderId).GetAwaiter().GetResult().Value;
                if (payment == null)
                {
                    return new ApplicationResponse<PaymentResponse>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Payment not found for this order"
                    };
                }

                var paymentDto = PaymentResponse.MapFromPayment(payment);
                return new ApplicationResponse<PaymentResponse>
                {
                    IsSuccess = true,
                    Value = paymentDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment by order ID: {OrderId}", orderId);
                return new ApplicationResponse<PaymentResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "An error occurred while retrieving payment"
                };
            }
        }

        public async Task<ApplicationResponse<IEnumerable<PaymentResponse>>> GetAllPaymentsAsync()
        {
            try
            {
                var payments = _paymentRepository.GetAllAsync().GetAwaiter().GetResult().Value;
                var paymentDtos = PaymentResponse.MapFromPayment(payments);

                return new ApplicationResponse<IEnumerable<PaymentResponse>>
                {
                    IsSuccess = true,
                    Value = paymentDtos
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all payments");
                return new ApplicationResponse<IEnumerable<PaymentResponse>>
                {
                    IsSuccess = false,
                    ErrorMessage = "An error occurred while retrieving payments"
                };
            }
        }

        public async Task<ApplicationResponse<IEnumerable<PaymentResponse>>> GetPaymentsByUserIdAsync(int userId)
        {
            try
            {
                var payments = _paymentRepository.GetByUserIdAsync(userId).GetAwaiter().GetResult().Value;
                var paymentDtos = PaymentResponse.MapFromPayment(payments);

                return new ApplicationResponse<IEnumerable<PaymentResponse>>
                {
                    IsSuccess = true,
                    Value = paymentDtos
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payments by user ID: {UserId}", userId);
                return new ApplicationResponse<IEnumerable<PaymentResponse>>
                {
                    IsSuccess = false,
                    ErrorMessage = "An error occurred while retrieving user payments"
                };
            }
        }

        public async Task<ApplicationResponse<IEnumerable<PaymentResponse>>> GetPaymentsByStatusAsync(PaymentStatus status)
        {
            try
            {
                var payments = _paymentRepository.GetByStatusAsync(status).GetAwaiter().GetResult().Value;
                var paymentDtos = PaymentResponse.MapFromPayment(payments);

                return new ApplicationResponse<IEnumerable<PaymentResponse>>
                {
                    IsSuccess = true,
                    Value = paymentDtos
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payments by status: {Status}", status);
                return new ApplicationResponse<IEnumerable<PaymentResponse>>
                {
                    IsSuccess = false,
                    ErrorMessage = "An error occurred while retrieving payments by status"
                };
            }
        }

        public async Task<ApplicationResponse<IEnumerable<PaymentResponse>>> GetPaymentsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var payments = _paymentRepository.GetByDateRangeAsync(startDate, endDate).GetAwaiter().GetResult().Value;
                var paymentDtos = PaymentResponse.MapFromPayment(payments);

                return new ApplicationResponse<IEnumerable<PaymentResponse>>
                {
                    IsSuccess = true,
                    Value = paymentDtos
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payments by date range");
                return new ApplicationResponse<IEnumerable<PaymentResponse>>
                {
                    IsSuccess = false,
                    ErrorMessage = "An error occurred while retrieving payments by date range"
                };
            }
        }

        public async Task<ApplicationResponse<IEnumerable<PaymentResponse>>> GetPaginatedPaymentsAsync(int pageNumber, int pageSize)
        {
            try
            {
                var payments = _paymentRepository.GetPaginatedAsync(pageNumber, pageSize).GetAwaiter().GetResult().Value;
                var paymentDtos = PaymentResponse.MapFromPayment(payments);

                return new ApplicationResponse<IEnumerable<PaymentResponse>>
                {
                    IsSuccess = true,
                    Value = paymentDtos
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginated payments");
                return new ApplicationResponse<IEnumerable<PaymentResponse>>
                {
                    IsSuccess = false,
                    ErrorMessage = "An error occurred while retrieving paginated payments"
                };
            }
        }

        public async Task<ApplicationResponse<IEnumerable<PaymentResponse>>> GetPaginatedPaymentsByUserIdAsync(int userId, int pageNumber, int pageSize)
        {
            try
            {
                var payments = _paymentRepository.GetPaginatedByUserIdAsync(userId, pageNumber, pageSize).GetAwaiter().GetResult().Value;
                var paymentDtos = PaymentResponse.MapFromPayment(payments);

                return new ApplicationResponse<IEnumerable<PaymentResponse>>
                {
                    IsSuccess = true,
                    Value = paymentDtos
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginated payments by user ID: {UserId}", userId);
                return new ApplicationResponse<IEnumerable<PaymentResponse>>
                {
                    IsSuccess = false,
                    ErrorMessage = "An error occurred while retrieving user payments"
                };
            }
        }

        public async Task<ApplicationResponse<PaymentResponseDto>> UpdatePaymentStatusAsync(int id, PaymentStatus status, string? notes = null)
        {
            try
            {
                var payment = _paymentRepository.GetByIdAsync(id).GetAwaiter().GetResult().Value;
                if (payment == null)
                {
                    return new ApplicationResponse<PaymentResponseDto>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Payment not found"
                    };
                }

                payment.Status = status;
                payment.Notes = notes ?? payment.Notes;

                switch (status)
                {
                    case PaymentStatus.Completed:
                        payment.CompletedAt = DateTime.UtcNow;
                        break;
                    case PaymentStatus.Failed:
                        payment.FailedAt = DateTime.UtcNow;
                        break;
                    case PaymentStatus.Refunded:
                        payment.RefundedAt = DateTime.UtcNow;
                        break;
                }

                var updatedPayment = _paymentRepository.UpdateAsync(payment).GetAwaiter().GetResult().Value;
                var paymentDto = PaymentResponse.MapFromPayment(updatedPayment);

                var response = new PaymentResponseDto
                {
                    Success = true,
                    Message = "Payment status updated successfully",
                    Payment = paymentDto,
                    TransactionId = payment.TransactionId,
                    Status = payment.Status
                };

                return new ApplicationResponse<PaymentResponseDto>
                {
                    IsSuccess = true,
                    Value = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment status for ID: {PaymentId}", id);
                return new ApplicationResponse<PaymentResponseDto>
                {
                    IsSuccess = false,
                    ErrorMessage = "An error occurred while updating payment status"
                };
            }
        }

        public async Task<ApplicationResponse<PaymentResponseDto>> RefundPaymentAsync(int id, decimal? refundAmount = null, string? reason = null)
        {
            try
            {
                var payment = _paymentRepository.GetByIdAsync(id).GetAwaiter().GetResult().Value;
                if (payment == null)
                {
                    return new ApplicationResponse<PaymentResponseDto>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Payment not found"
                    };
                }

                if (payment.Status != PaymentStatus.Completed)
                {
                    return new ApplicationResponse<PaymentResponseDto>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Only completed payments can be refunded"
                    };
                }

                var amountToRefund = refundAmount ?? payment.Amount;
                if (amountToRefund <= 0 || amountToRefund > payment.Amount)
                {
                    return new ApplicationResponse<PaymentResponseDto>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Refund amount must be positive and cannot exceed payment amount"
                    };
                }

                var refundResult = await ProcessRefundWithProvider(payment, amountToRefund);

                if (refundResult.Value.Success)
                {
                    payment.Status = amountToRefund == payment.Amount ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded;
                    payment.RefundedAt = DateTime.UtcNow;
                    payment.Notes = reason ?? payment.Notes;
                }

                var updatedPayment = _paymentRepository.UpdateAsync(payment).GetAwaiter().GetResult().Value;
                var paymentDto = PaymentResponse.MapFromPayment(updatedPayment);

                var response = new PaymentResponseDto
                {
                    Success = refundResult.Value.Success,
                    Message = refundResult.Value.Success ? "Payment refunded successfully" : "Refund failed",
                    Payment = paymentDto,
                    TransactionId = payment.TransactionId,
                    Status = payment.Status
                };

                return new ApplicationResponse<PaymentResponseDto>
                {
                    IsSuccess = true,
                    Value = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refunding payment for ID: {PaymentId}", id);
                return new ApplicationResponse<PaymentResponseDto>
                {
                    IsSuccess = false,
                    ErrorMessage = "An error occurred while processing refund"
                };
            }
        }

        public async Task<ApplicationResponse<bool>> DeletePaymentAsync(int id)
        {
            try
            {
                var result = _paymentRepository.DeleteAsync(id).GetAwaiter().GetResult().Value;
                return new ApplicationResponse<bool>
                {
                    IsSuccess = result,
                    Value = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting payment for ID: {PaymentId}", id);
                return new ApplicationResponse<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = "An error occurred while deleting payment",
                    Value = false
                };
            }
        }

        public async Task<ApplicationResponse<int>> GetTotalPaymentCountAsync()
        {
            try
            {
                var count = _paymentRepository.GetTotalCountAsync().GetAwaiter().GetResult().Value;
                return new ApplicationResponse<int> { IsSuccess = true, Value = count };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total payment count");
                return new ApplicationResponse<int> { IsSuccess = false, ErrorMessage = "An error occurred while retrieving payment count" };
            }
        }

        public async Task<ApplicationResponse<int>> GetPaymentCountByUserIdAsync(int userId)
        {
            try
            {
                var count = _paymentRepository.GetCountByUserIdAsync(userId).GetAwaiter().GetResult().Value;
                return new ApplicationResponse<int> { IsSuccess = true, Value = count };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment count by user ID: {UserId}", userId);
                return new ApplicationResponse<int> { IsSuccess = false, ErrorMessage = "An error occurred while retrieving user payment count" };
            }
        }

        public async Task<ApplicationResponse<int>> GetPaymentCountByStatusAsync(PaymentStatus status)
        {
            try
            {
                var count = _paymentRepository.GetCountByStatusAsync(status).GetAwaiter().GetResult().Value;
                return new ApplicationResponse<int> { IsSuccess = true, Value = count };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment count by status: {Status}", status);
                return new ApplicationResponse<int> { IsSuccess = false, ErrorMessage = "An error occurred while retrieving payment count by status" };
            }
        }

        public async Task<ApplicationResponse<decimal>> GetTotalAmountByUserIdAsync(int userId)
        {
            try
            {
                var total = _paymentRepository.GetTotalAmountByUserIdAsync(userId).GetAwaiter().GetResult().Value;
                return new ApplicationResponse<decimal> { IsSuccess = true, Value = total };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total amount by user ID: {UserId}", userId);
                return new ApplicationResponse<decimal> { IsSuccess = false, ErrorMessage = "An error occurred while retrieving total amount" };
            }
        }

        public async Task<ApplicationResponse<decimal>> GetTotalAmountByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var total = _paymentRepository.GetTotalAmountByDateRangeAsync(startDate, endDate).GetAwaiter().GetResult().Value;
                return new ApplicationResponse<decimal> { IsSuccess = true, Value = total };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total amount by date range");
                return new ApplicationResponse<decimal> { IsSuccess = false, ErrorMessage = "An error occurred while retrieving total amount" };
            }
        }

        public async Task<ApplicationResponse<IEnumerable<PaymentResponse>>> SearchPaymentsAsync(string searchTerm, int pageNumber, int pageSize)
        {
            try
            {
                var payments = _paymentRepository.SearchAsync(searchTerm, pageNumber, pageSize).GetAwaiter().GetResult().Value;
                var paymentDtos = PaymentResponse.MapFromPayment(payments);

                return new ApplicationResponse<IEnumerable<PaymentResponse>> { IsSuccess = true, Value = paymentDtos };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching payments with term: {SearchTerm}", searchTerm);
                return new ApplicationResponse<IEnumerable<PaymentResponse>> { IsSuccess = false, ErrorMessage = "An error occurred while searching payments" };
            }
        }

        public async Task<ApplicationResponse<PaymentResponseDto>> VerifyPaymentWithProviderAsync(string transactionId)
        {
            try
            {
                var payment = _paymentRepository.GetByTransactionIdAsync(transactionId).GetAwaiter().GetResult().Value;
                if (payment == null)
                {
                    return new ApplicationResponse<PaymentResponseDto> { IsSuccess = false, ErrorMessage = "Payment not found" };
                }

                var verificationResult = VerifyWithPaymentProvider(payment).GetAwaiter().GetResult().Value;

                if (verificationResult.Success && payment.Status != PaymentStatus.Completed)
                {
                    payment.Status = PaymentStatus.Completed;
                    payment.CompletedAt = DateTime.UtcNow;
                    await _paymentRepository.UpdateAsync(payment);
                }

                var paymentDto = PaymentResponse.MapFromPayment(payment);
                var response = new PaymentResponseDto
                {
                    Success = verificationResult.Success,
                    Message = verificationResult.Message,
                    Payment = paymentDto,
                    TransactionId = transactionId,
                    Status = payment.Status
                };

                return new ApplicationResponse<PaymentResponseDto> { IsSuccess = true, Value = response };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying payment with transaction ID: {TransactionId}", transactionId);
                return new ApplicationResponse<PaymentResponseDto> { IsSuccess = false, ErrorMessage = "An error occurred while verifying payment" };
            }
        }

        public ApplicationResponse<string> GenerateTransactionIdAsync()
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var randomPart = Guid.NewGuid().ToString("N")[..8].ToUpper();
            return new ApplicationResponse<string> { IsSuccess = true, Value = $"PAY-{timestamp}-{randomPart}" };
        }

        private ApplicationResponse<decimal> CalculateProcessingFee(decimal amount, string provider)
        {
            return new ApplicationResponse<decimal>()
            {
                Value = provider.ToLower() switch
                {
                    "stripe" => amount * 0.029m + 0.30m,
                    "paypal" => amount * 0.034m + 0.30m,
                    _ => amount * 0.025m
                },
                IsSuccess = true
            };
        }

        private async Task<ApplicationResponse<ProviderResult>> ProcessWithPaymentProvider(Payment payment)
        {
            await Task.Delay(100);
            var random = new Random();
            var success = random.NextDouble() > 0.1;

            return new ApplicationResponse<ProviderResult>()
            {
                Value = new ProviderResult()
                {
                    Success = success,
                    ProviderTransactionId = success ? $"pi_{Guid.NewGuid().ToString("N")[..24]}" : null,
                    ErrorMessage = success ? null : "Payment declined by bank",
                    FailureReason = success ? null : "insufficient_funds"
                }
            };          
        }

        private async Task<ApplicationResponse<ProviderResult>> ProcessRefundWithProvider(Payment payment, decimal amount)
        {
            await Task.Delay(100);
            return new ApplicationResponse<ProviderResult>
            {
                Value = new ProviderResult
                {
                    Success = true,
                    ProviderTransactionId = $"re_{Guid.NewGuid().ToString("N")[..24]}",
                    Message = "Refund processed successfully"
                }
            };
        }

        private async Task<ApplicationResponse<ProviderResult>> VerifyWithPaymentProvider(Payment payment)
        {
            await Task.Delay(50);
            return new ApplicationResponse<ProviderResult>
            {
                Value = new ProviderResult
                {
                    Success = payment.Status == PaymentStatus.Completed || payment.Status == PaymentStatus.Processing,
                    Message = "Payment verification completed"
                }
            };
        }

        private class ProviderResult
        {
            public bool Success { get; set; }
            public string? ProviderTransactionId { get; set; }
            public string? ErrorMessage { get; set; }
            public string? FailureReason { get; set; }
            public string? Message { get; set; }
        }
    }
}