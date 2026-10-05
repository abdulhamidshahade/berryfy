using Berryfy.Domain.Constants;
using Berryfy.Domain.Entities;
using Berryfy.Domain.Entities.PaymentEntities;

namespace Berryfy.Domain.Repositories.PaymentInterfaces
{
    public interface IPaymentRepository
    {
        Task<InfrastructureResponse<Payment>> GetByIdAsync(int id);
        Task<InfrastructureResponse<Payment?>> GetByTransactionIdAsync(string transactionId);
        Task<InfrastructureResponse<Payment?>> GetByOrderIdAsync(int orderId);
        Task<InfrastructureResponse<IEnumerable<Payment>>> GetAllAsync();
        Task<InfrastructureResponse<IEnumerable<Payment>>> GetByUserIdAsync(int userId);
        Task<InfrastructureResponse<IEnumerable<Payment>>> GetByStatusAsync(PaymentStatus status);
        Task<InfrastructureResponse<IEnumerable<Payment>>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<InfrastructureResponse<IEnumerable<Payment>>> GetPaginatedAsync(int pageNumber, int pageSize);
        Task<InfrastructureResponse<IEnumerable<Payment>>> GetPaginatedByUserIdAsync(int userId, int pageNumber, int pageSize);
        Task<InfrastructureResponse<Payment>> CreateAsync(Payment payment);
        Task<InfrastructureResponse<Payment>> UpdateAsync(Payment payment);
        Task<InfrastructureResponse<bool>> DeleteAsync(int id);
        Task<InfrastructureResponse<int>> GetTotalCountAsync();
        Task<InfrastructureResponse<int>> GetCountByUserIdAsync(int userId);
        Task<InfrastructureResponse<int>> GetCountByStatusAsync(PaymentStatus status);
        Task<InfrastructureResponse<decimal>> GetTotalAmountByUserIdAsync(int userId);
        Task<InfrastructureResponse<decimal>> GetTotalAmountByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<InfrastructureResponse<IEnumerable<Payment>>> SearchAsync(string searchTerm, int pageNumber, int pageSize);
    }
}
