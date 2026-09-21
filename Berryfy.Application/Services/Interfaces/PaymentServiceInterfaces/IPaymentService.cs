using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.PaymentDtos.Requests;
using Berryfy.Application.Dtos.PaymentDtos.Responses;
using Berryfy.Domain.Constants;

namespace Berryfy.Application.Services.Interfaces.PaymentServiceInterfaces
{
    public interface IPaymentService
    {
        Task<ResponseDto<PaymentResponseDto>> ProcessPaymentAsync(CreatePayment createPaymentDto, int? userId, string? sessionId);
        Task<ResponseDto<PaymentResponse>> GetPaymentByIdAsync(int id);
        Task<ResponseDto<PaymentResponse>> GetPaymentByTransactionIdAsync(string transactionId);
        Task<ResponseDto<PaymentResponse>> GetPaymentByOrderIdAsync(int orderId);
        Task<ResponseDto<IEnumerable<PaymentResponse>>> GetAllPaymentsAsync();
        Task<ResponseDto<IEnumerable<PaymentResponse>>> GetPaymentsByUserIdAsync(int userId);
        Task<ResponseDto<IEnumerable<PaymentResponse>>> GetPaymentsByStatusAsync(PaymentStatus status);
        Task<ResponseDto<IEnumerable<PaymentResponse>>> GetPaymentsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<ResponseDto<IEnumerable<PaymentResponse>>> GetPaginatedPaymentsAsync(int pageNumber, int pageSize);
        Task<ResponseDto<IEnumerable<PaymentResponse>>> GetPaginatedPaymentsByUserIdAsync(int userId, int pageNumber, int pageSize);
        Task<ResponseDto<PaymentResponseDto>> UpdatePaymentStatusAsync(int id, PaymentStatus status, string? notes = null);
        Task<ResponseDto<PaymentResponseDto>> RefundPaymentAsync(int id, decimal? refundAmount = null, string? reason = null);
        Task<ResponseDto<bool>> DeletePaymentAsync(int id);
        Task<ResponseDto<int>> GetTotalPaymentCountAsync();
        Task<ResponseDto<int>> GetPaymentCountByUserIdAsync(int userId);
        Task<ResponseDto<int>> GetPaymentCountByStatusAsync(PaymentStatus status);
        Task<ResponseDto<decimal>> GetTotalAmountByUserIdAsync(int userId);
        Task<ResponseDto<decimal>> GetTotalAmountByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<ResponseDto<IEnumerable<PaymentResponse>>> SearchPaymentsAsync(string searchTerm, int pageNumber, int pageSize);
        Task<ResponseDto<PaymentResponseDto>> VerifyPaymentWithProviderAsync(string transactionId);
        Task<string> GenerateTransactionIdAsync();
    }
}
