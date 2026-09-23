using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.PaymentDtos.Requests;
using Berryfy.Application.Dtos.PaymentDtos.Responses;
using Berryfy.Domain.Constants;

namespace Berryfy.Application.Services.Interfaces.PaymentServiceInterfaces
{
    public interface IPaymentService
    {
        Task<ApiResponse<PaymentResponseDto>> ProcessPaymentAsync(CreatePayment createPaymentDto, int? userId, string? sessionId);
        Task<ApiResponse<PaymentResponse>> GetPaymentByIdAsync(int id);
        Task<ApiResponse<PaymentResponse>> GetPaymentByTransactionIdAsync(string transactionId);
        Task<ApiResponse<PaymentResponse>> GetPaymentByOrderIdAsync(int orderId);
        Task<ApiResponse<IEnumerable<PaymentResponse>>> GetAllPaymentsAsync();
        Task<ApiResponse<IEnumerable<PaymentResponse>>> GetPaymentsByUserIdAsync(int userId);
        Task<ApiResponse<IEnumerable<PaymentResponse>>> GetPaymentsByStatusAsync(PaymentStatus status);
        Task<ApiResponse<IEnumerable<PaymentResponse>>> GetPaymentsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<ApiResponse<IEnumerable<PaymentResponse>>> GetPaginatedPaymentsAsync(int pageNumber, int pageSize);
        Task<ApiResponse<IEnumerable<PaymentResponse>>> GetPaginatedPaymentsByUserIdAsync(int userId, int pageNumber, int pageSize);
        Task<ApiResponse<PaymentResponseDto>> UpdatePaymentStatusAsync(int id, PaymentStatus status, string? notes = null);
        Task<ApiResponse<PaymentResponseDto>> RefundPaymentAsync(int id, decimal? refundAmount = null, string? reason = null);
        Task<ApiResponse<bool>> DeletePaymentAsync(int id);
        Task<ApiResponse<int>> GetTotalPaymentCountAsync();
        Task<ApiResponse<int>> GetPaymentCountByUserIdAsync(int userId);
        Task<ApiResponse<int>> GetPaymentCountByStatusAsync(PaymentStatus status);
        Task<ApiResponse<decimal>> GetTotalAmountByUserIdAsync(int userId);
        Task<ApiResponse<decimal>> GetTotalAmountByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<ApiResponse<IEnumerable<PaymentResponse>>> SearchPaymentsAsync(string searchTerm, int pageNumber, int pageSize);
        Task<ApiResponse<PaymentResponseDto>> VerifyPaymentWithProviderAsync(string transactionId);
        Task<string> GenerateTransactionIdAsync();
    }
}
