using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.PaymentDtos.Requests;
using Berryfy.Application.Dtos.PaymentDtos.Responses;
using Berryfy.Domain.Constants;

namespace Berryfy.Application.Services.Interfaces.PaymentServiceInterfaces
{
    public interface IPaymentService
    {
        Task<ApplicationResponse<PaymentResponseDto>> ProcessPaymentAsync(CreatePayment createPaymentDto, int? userId, string? sessionId);
        Task<ApplicationResponse<PaymentResponse>> GetPaymentByIdAsync(int id);
        Task<ApplicationResponse<PaymentResponse>> GetPaymentByTransactionIdAsync(string transactionId);
        Task<ApplicationResponse<PaymentResponse>> GetPaymentByOrderIdAsync(int orderId);
        Task<ApplicationResponse<IEnumerable<PaymentResponse>>> GetAllPaymentsAsync();
        Task<ApplicationResponse<IEnumerable<PaymentResponse>>> GetPaymentsByUserIdAsync(int userId);
        Task<ApplicationResponse<IEnumerable<PaymentResponse>>> GetPaymentsByStatusAsync(PaymentStatus status);
        Task<ApplicationResponse<IEnumerable<PaymentResponse>>> GetPaymentsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<ApplicationResponse<IEnumerable<PaymentResponse>>> GetPaginatedPaymentsAsync(int pageNumber, int pageSize);
        Task<ApplicationResponse<IEnumerable<PaymentResponse>>> GetPaginatedPaymentsByUserIdAsync(int userId, int pageNumber, int pageSize);
        Task<ApplicationResponse<PaymentResponseDto>> UpdatePaymentStatusAsync(int id, PaymentStatus status, string? notes = null);
        Task<ApplicationResponse<PaymentResponseDto>> RefundPaymentAsync(int id, decimal? refundAmount = null, string? reason = null);
        Task<ApplicationResponse<bool>> DeletePaymentAsync(int id);
        Task<ApplicationResponse<int>> GetTotalPaymentCountAsync();
        Task<ApplicationResponse<int>> GetPaymentCountByUserIdAsync(int userId);
        Task<ApplicationResponse<int>> GetPaymentCountByStatusAsync(PaymentStatus status);
        Task<ApplicationResponse<decimal>> GetTotalAmountByUserIdAsync(int userId);
        Task<ApplicationResponse<decimal>> GetTotalAmountByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<ApplicationResponse<IEnumerable<PaymentResponse>>> SearchPaymentsAsync(string searchTerm, int pageNumber, int pageSize);
        Task<ApplicationResponse<PaymentResponseDto>> VerifyPaymentWithProviderAsync(string transactionId);
        ApplicationResponse<string> GenerateTransactionIdAsync();
    }
}
