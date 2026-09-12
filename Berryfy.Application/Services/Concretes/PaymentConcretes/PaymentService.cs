using AutoMapper;
using Berryfy.Application.Dtos.PaymentDtos;
using Berryfy.Application.Dtos;
using Berryfy.Application.Services.Interfaces.PaymentServiceInterfaces;
using Berryfy.Domain.Constants;
using Berryfy.Domain.Entities.PaymentEntities;
using Berryfy.Domain.Repositories.PaymentInterfaces;
using Microsoft.Extensions.Logging;

namespace Berryfy.Application.Services.Concretes.PaymentConcretes
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            IPaymentRepository paymentRepository,
            IMapper mapper,
            ILogger<PaymentService> logger)
        {
            _paymentRepository = paymentRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ResponseDto<PaymentResponseDto>> ProcessPaymentAsync(CreatePaymentDto createPaymentDto, int? userId, string? sessionId)
        {
            try
            {
                var transactionId = await GenerateTransactionIdAsync();
                var processingFee = CalculateProcessingFee(createPaymentDto.Amount, createPaymentDto.Provider);
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

                var providerResult = await ProcessWithPaymentProvider(payment);

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

                var createdPayment = await _paymentRepository.CreateAsync(payment);
                var paymentDto = _mapper.Map<PaymentDto>(createdPayment);

                var response = new PaymentResponseDto
                {
                    Success = providerResult.Success,
                    Message = providerResult.Success ? "Payment processed successfully" : "Payment failed",
                    Payment = paymentDto,
                    TransactionId = transactionId,
                    Status = payment.Status,
                    RedirectUrl = providerResult.Success ? "/payment/success" : "/payment/failed"
                };

                return new ResponseDto<PaymentResponseDto>
                {
                    IsSuccess = true,
                    StatusMessage = "Payment processing completed",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment");
                return new ResponseDto<PaymentResponseDto>
                {
                    IsSuccess = false,
                    StatusMessage = "An error occurred while processing payment"
                };
            }
        }

        public async Task<ResponseDto<PaymentDto>> GetPaymentByIdAsync(int id)
        {
            try
            {
                var payment = await _paymentRepository.GetByIdAsync(id);
                if (payment == null)
                {
                    return new ResponseDto<PaymentDto>
                    {
                        IsSuccess = false,
                        StatusMessage = "Payment not found"
                    };
                }

                var paymentDto = _mapper.Map<PaymentDto>(payment);
                return new ResponseDto<PaymentDto>
                {
                    IsSuccess = true,
                    Data = paymentDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment by ID: {PaymentId}", id);
                return new ResponseDto<PaymentDto>
                {
                    IsSuccess = false,
                    StatusMessage = "An error occurred while retrieving payment"
                };
            }
        }

        public async Task<ResponseDto<PaymentDto>> GetPaymentByTransactionIdAsync(string transactionId)
        {
            try
            {
                var payment = await _paymentRepository.GetByTransactionIdAsync(transactionId);
                if (payment == null)
                {
                    return new ResponseDto<PaymentDto>
                    {
                        IsSuccess = false,
                        StatusMessage = "Payment not found"
                    };
                }

                var paymentDto = _mapper.Map<PaymentDto>(payment);
                return new ResponseDto<PaymentDto>
                {
                    IsSuccess = true,
                    Data = paymentDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment by transaction ID: {TransactionId}", transactionId);
                return new ResponseDto<PaymentDto>
                {
                    IsSuccess = false,
                    StatusMessage = "An error occurred while retrieving payment"
                };
            }
        }

        public async Task<ResponseDto<PaymentDto>> GetPaymentByOrderIdAsync(int orderId)
        {
            try
            {
                var payment = await _paymentRepository.GetByOrderIdAsync(orderId);
                if (payment == null)
                {
                    return new ResponseDto<PaymentDto>
                    {
                        IsSuccess = false,
                        StatusMessage = "Payment not found for this order"
                    };
                }

                var paymentDto = _mapper.Map<PaymentDto>(payment);
                return new ResponseDto<PaymentDto>
                {
                    IsSuccess = true,
                    Data = paymentDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment by order ID: {OrderId}", orderId);
                return new ResponseDto<PaymentDto>
                {
                    IsSuccess = false,
                    StatusMessage = "An error occurred while retrieving payment"
                };
            }
        }

        public async Task<ResponseDto<IEnumerable<PaymentDto>>> GetAllPaymentsAsync()
        {
            try
            {
                var payments = await _paymentRepository.GetAllAsync();
                var paymentDtos = _mapper.Map<IEnumerable<PaymentDto>>(payments);

                return new ResponseDto<IEnumerable<PaymentDto>>
                {
                    IsSuccess = true,
                    Data = paymentDtos
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all payments");
                return new ResponseDto<IEnumerable<PaymentDto>>
                {
                    IsSuccess = false,
                    StatusMessage = "An error occurred while retrieving payments"
                };
            }
        }

        public async Task<ResponseDto<IEnumerable<PaymentDto>>> GetPaymentsByUserIdAsync(int userId)
        {
            try
            {
                var payments = await _paymentRepository.GetByUserIdAsync(userId);
                var paymentDtos = _mapper.Map<IEnumerable<PaymentDto>>(payments);

                return new ResponseDto<IEnumerable<PaymentDto>>
                {
                    IsSuccess = true,
                    Data = paymentDtos
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payments by user ID: {UserId}", userId);
                return new ResponseDto<IEnumerable<PaymentDto>>
                {
                    IsSuccess = false,
                    StatusMessage = "An error occurred while retrieving user payments"
                };
            }
        }

        public async Task<ResponseDto<IEnumerable<PaymentDto>>> GetPaymentsByStatusAsync(PaymentStatus status)
        {
            try
            {
                var payments = await _paymentRepository.GetByStatusAsync(status);
                var paymentDtos = _mapper.Map<IEnumerable<PaymentDto>>(payments);

                return new ResponseDto<IEnumerable<PaymentDto>>
                {
                    IsSuccess = true,
                    Data = paymentDtos
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payments by status: {Status}", status);
                return new ResponseDto<IEnumerable<PaymentDto>>
                {
                    IsSuccess = false,
                    StatusMessage = "An error occurred while retrieving payments by status"
                };
            }
        }

        public async Task<ResponseDto<IEnumerable<PaymentDto>>> GetPaymentsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var payments = await _paymentRepository.GetByDateRangeAsync(startDate, endDate);
                var paymentDtos = _mapper.Map<IEnumerable<PaymentDto>>(payments);

                return new ResponseDto<IEnumerable<PaymentDto>>
                {
                    IsSuccess = true,
                    Data = paymentDtos
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payments by date range");
                return new ResponseDto<IEnumerable<PaymentDto>>
                {
                    IsSuccess = false,
                    StatusMessage = "An error occurred while retrieving payments by date range"
                };
            }
        }

        public async Task<ResponseDto<IEnumerable<PaymentDto>>> GetPaginatedPaymentsAsync(int pageNumber, int pageSize)
        {
            try
            {
                var payments = await _paymentRepository.GetPaginatedAsync(pageNumber, pageSize);
                var paymentDtos = _mapper.Map<IEnumerable<PaymentDto>>(payments);

                return new ResponseDto<IEnumerable<PaymentDto>>
                {
                    IsSuccess = true,
                    Data = paymentDtos
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginated payments");
                return new ResponseDto<IEnumerable<PaymentDto>>
                {
                    IsSuccess = false,
                    StatusMessage = "An error occurred while retrieving paginated payments"
                };
            }
        }

        public async Task<ResponseDto<IEnumerable<PaymentDto>>> GetPaginatedPaymentsByUserIdAsync(int userId, int pageNumber, int pageSize)
        {
            try
            {
                var payments = await _paymentRepository.GetPaginatedByUserIdAsync(userId, pageNumber, pageSize);
                var paymentDtos = _mapper.Map<IEnumerable<PaymentDto>>(payments);

                return new ResponseDto<IEnumerable<PaymentDto>>
                {
                    IsSuccess = true,
                    Data = paymentDtos
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginated payments by user ID: {UserId}", userId);
                return new ResponseDto<IEnumerable<PaymentDto>>
                {
                    IsSuccess = false,
                    StatusMessage = "An error occurred while retrieving user payments"
                };
            }
        }

        public async Task<ResponseDto<PaymentResponseDto>> UpdatePaymentStatusAsync(int id, PaymentStatus status, string? notes = null)
        {
            try
            {
                var payment = await _paymentRepository.GetByIdAsync(id);
                if (payment == null)
                {
                    return new ResponseDto<PaymentResponseDto>
                    {
                        IsSuccess = false,
                        StatusMessage = "Payment not found"
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

                var updatedPayment = await _paymentRepository.UpdateAsync(payment);
                var paymentDto = _mapper.Map<PaymentDto>(updatedPayment);

                var response = new PaymentResponseDto
                {
                    Success = true,
                    Message = "Payment status updated successfully",
                    Payment = paymentDto,
                    TransactionId = payment.TransactionId,
                    Status = payment.Status
                };

                return new ResponseDto<PaymentResponseDto>
                {
                    IsSuccess = true,
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment status for ID: {PaymentId}", id);
                return new ResponseDto<PaymentResponseDto>
                {
                    IsSuccess = false,
                    StatusMessage = "An error occurred while updating payment status"
                };
            }
        }

        public async Task<ResponseDto<PaymentResponseDto>> RefundPaymentAsync(int id, decimal? refundAmount = null, string? reason = null)
        {
            try
            {
                var payment = await _paymentRepository.GetByIdAsync(id);
                if (payment == null)
                {
                    return new ResponseDto<PaymentResponseDto>
                    {
                        IsSuccess = false,
                        StatusMessage = "Payment not found"
                    };
                }

                if (payment.Status != PaymentStatus.Completed)
                {
                    return new ResponseDto<PaymentResponseDto>
                    {
                        IsSuccess = false,
                        StatusMessage = "Only completed payments can be refunded"
                    };
                }

                var amountToRefund = refundAmount ?? payment.Amount;
                if (amountToRefund <= 0 || amountToRefund > payment.Amount)
                {
                    return new ResponseDto<PaymentResponseDto>
                    {
                        IsSuccess = false,
                        StatusMessage = "Refund amount must be positive and cannot exceed payment amount"
                    };
                }

                var refundResult = await ProcessRefundWithProvider(payment, amountToRefund);

                if (refundResult.Success)
                {
                    payment.Status = amountToRefund == payment.Amount ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded;
                    payment.RefundedAt = DateTime.UtcNow;
                    payment.Notes = reason ?? payment.Notes;
                }

                var updatedPayment = await _paymentRepository.UpdateAsync(payment);
                var paymentDto = _mapper.Map<PaymentDto>(updatedPayment);

                var response = new PaymentResponseDto
                {
                    Success = refundResult.Success,
                    Message = refundResult.Success ? "Payment refunded successfully" : "Refund failed",
                    Payment = paymentDto,
                    TransactionId = payment.TransactionId,
                    Status = payment.Status
                };

                return new ResponseDto<PaymentResponseDto>
                {
                    IsSuccess = true,
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refunding payment for ID: {PaymentId}", id);
                return new ResponseDto<PaymentResponseDto>
                {
                    IsSuccess = false,
                    StatusMessage = "An error occurred while processing refund"
                };
            }
        }

        public async Task<ResponseDto<bool>> DeletePaymentAsync(int id)
        {
            try
            {
                var result = await _paymentRepository.DeleteAsync(id);
                return new ResponseDto<bool>
                {
                    IsSuccess = result,
                    StatusMessage = result ? "Payment deleted successfully" : "Payment not found or could not be deleted",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting payment for ID: {PaymentId}", id);
                return new ResponseDto<bool>
                {
                    IsSuccess = false,
                    StatusMessage = "An error occurred while deleting payment",
                    Data = false
                };
            }
        }

        public async Task<ResponseDto<int>> GetTotalPaymentCountAsync()
        {
            try
            {
                var count = await _paymentRepository.GetTotalCountAsync();
                return new ResponseDto<int> { IsSuccess = true, Data = count };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total payment count");
                return new ResponseDto<int> { IsSuccess = false, StatusMessage = "An error occurred while retrieving payment count" };
            }
        }

        public async Task<ResponseDto<int>> GetPaymentCountByUserIdAsync(int userId)
        {
            try
            {
                var count = await _paymentRepository.GetCountByUserIdAsync(userId);
                return new ResponseDto<int> { IsSuccess = true, Data = count };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment count by user ID: {UserId}", userId);
                return new ResponseDto<int> { IsSuccess = false, StatusMessage = "An error occurred while retrieving user payment count" };
            }
        }

        public async Task<ResponseDto<int>> GetPaymentCountByStatusAsync(PaymentStatus status)
        {
            try
            {
                var count = await _paymentRepository.GetCountByStatusAsync(status);
                return new ResponseDto<int> { IsSuccess = true, Data = count };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment count by status: {Status}", status);
                return new ResponseDto<int> { IsSuccess = false, StatusMessage = "An error occurred while retrieving payment count by status" };
            }
        }

        public async Task<ResponseDto<decimal>> GetTotalAmountByUserIdAsync(int userId)
        {
            try
            {
                var total = await _paymentRepository.GetTotalAmountByUserIdAsync(userId);
                return new ResponseDto<decimal> { IsSuccess = true, Data = total };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total amount by user ID: {UserId}", userId);
                return new ResponseDto<decimal> { IsSuccess = false, StatusMessage = "An error occurred while retrieving total amount" };
            }
        }

        public async Task<ResponseDto<decimal>> GetTotalAmountByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var total = await _paymentRepository.GetTotalAmountByDateRangeAsync(startDate, endDate);
                return new ResponseDto<decimal> { IsSuccess = true, Data = total };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total amount by date range");
                return new ResponseDto<decimal> { IsSuccess = false, StatusMessage = "An error occurred while retrieving total amount" };
            }
        }

        public async Task<ResponseDto<IEnumerable<PaymentDto>>> SearchPaymentsAsync(string searchTerm, int pageNumber, int pageSize)
        {
            try
            {
                var payments = await _paymentRepository.SearchAsync(searchTerm, pageNumber, pageSize);
                var paymentDtos = _mapper.Map<IEnumerable<PaymentDto>>(payments);

                return new ResponseDto<IEnumerable<PaymentDto>> { IsSuccess = true, Data = paymentDtos };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching payments with term: {SearchTerm}", searchTerm);
                return new ResponseDto<IEnumerable<PaymentDto>> { IsSuccess = false, StatusMessage = "An error occurred while searching payments" };
            }
        }

        public async Task<ResponseDto<PaymentResponseDto>> VerifyPaymentWithProviderAsync(string transactionId)
        {
            try
            {
                var payment = await _paymentRepository.GetByTransactionIdAsync(transactionId);
                if (payment == null)
                {
                    return new ResponseDto<PaymentResponseDto> { IsSuccess = false, StatusMessage = "Payment not found" };
                }

                var verificationResult = await VerifyWithPaymentProvider(payment);

                if (verificationResult.Success && payment.Status != PaymentStatus.Completed)
                {
                    payment.Status = PaymentStatus.Completed;
                    payment.CompletedAt = DateTime.UtcNow;
                    await _paymentRepository.UpdateAsync(payment);
                }

                var paymentDto = _mapper.Map<PaymentDto>(payment);
                var response = new PaymentResponseDto
                {
                    Success = verificationResult.Success,
                    Message = verificationResult.Message,
                    Payment = paymentDto,
                    TransactionId = transactionId,
                    Status = payment.Status
                };

                return new ResponseDto<PaymentResponseDto> { IsSuccess = true, Data = response };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying payment with transaction ID: {TransactionId}", transactionId);
                return new ResponseDto<PaymentResponseDto> { IsSuccess = false, StatusMessage = "An error occurred while verifying payment" };
            }
        }

        public async Task<string> GenerateTransactionIdAsync()
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var randomPart = Guid.NewGuid().ToString("N")[..8].ToUpper();
            return $"PAY-{timestamp}-{randomPart}";
        }

        private decimal CalculateProcessingFee(decimal amount, string provider)
        {
            return provider.ToLower() switch
            {
                "stripe" => amount * 0.029m + 0.30m,
                "paypal" => amount * 0.034m + 0.30m,
                _ => amount * 0.025m
            };
        }

        private async Task<ProviderResult> ProcessWithPaymentProvider(Payment payment)
        {
            await Task.Delay(100);
            var random = new Random();
            var success = random.NextDouble() > 0.1;

            return new ProviderResult
            {
                Success = success,
                ProviderTransactionId = success ? $"pi_{Guid.NewGuid().ToString("N")[..24]}" : null,
                ErrorMessage = success ? null : "Payment declined by bank",
                FailureReason = success ? null : "insufficient_funds"
            };
        }

        private async Task<ProviderResult> ProcessRefundWithProvider(Payment payment, decimal amount)
        {
            await Task.Delay(100);
            return new ProviderResult
            {
                Success = true,
                ProviderTransactionId = $"re_{Guid.NewGuid().ToString("N")[..24]}",
                Message = "Refund processed successfully"
            };
        }

        private async Task<ProviderResult> VerifyWithPaymentProvider(Payment payment)
        {
            await Task.Delay(50);
            return new ProviderResult
            {
                Success = payment.Status == PaymentStatus.Completed || payment.Status == PaymentStatus.Processing,
                Message = "Payment verification completed"
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
