using Berryfy.Domain.Constants;
using Berryfy.Domain.Entities.PaymentEntities;
using Berryfy.Domain.Repositories;
using Berryfy.Domain.Repositories.PaymentInterfaces;
using Berryfy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;


namespace Berryfy.Infrastructure.Repositories.PaymentConcretes
{
    class PaymentRepository : IPaymentRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork _unitOfWork;

        public PaymentRepository(ApplicationDbContext context, IUnitOfWork unitOfWork)
        {
            _context = context;
            _unitOfWork = unitOfWork;
        }

        public async Task<Payment?> GetByIdAsync(int id)
        {
            return await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Payment?> GetByTransactionIdAsync(string transactionId)
        {
            return await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.TransactionId == transactionId);
        }

        public async Task<Payment?> GetByOrderIdAsync(int orderId)
        {
            return await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Order)
                .Where(p => p.OrderId == orderId)
                .OrderByDescending(p => p.Status == PaymentStatus.Completed ||
                    p.Status == PaymentStatus.PartiallyRefunded || p.Status == PaymentStatus.Refunded)
                .ThenByDescending(p => p.Status == PaymentStatus.Processing)
                .ThenByDescending(p => p.CreatedAt)
                .ThenByDescending(p => p.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Payment>> GetAllAsync()
        {
            return await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Order)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Payment>> GetByUserIdAsync(int userId)
        {
            return await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Order)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Payment>> GetByStatusAsync(PaymentStatus status)
        {
            return await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Order)
                .Where(p => p.Status == status)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Payment>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Order)
                .Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Payment>> GetPaginatedAsync(int pageNumber, int pageSize)
        {
            return await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Order)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Payment>> GetPaginatedByUserIdAsync(int userId, int pageNumber, int pageSize)
        {
            return await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Order)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Payment> CreateAsync(Payment payment)
        {
            payment.CreatedAt = DateTime.UtcNow;
            payment.UpdatedAt = DateTime.UtcNow;

            await _context.Payments.AddAsync(payment);

            if (await _unitOfWork.SaveDbChangesAsync())
            {
                return payment;
            }

            throw new Exception("Failed to create payment");
        }

        public async Task<Payment> UpdateAsync(Payment payment)
        {
            payment.UpdatedAt = DateTime.UtcNow;
            _context.Payments.Update(payment);

            if (await _unitOfWork.SaveDbChangesAsync())
            {
                return payment;
            }

            throw new Exception("Failed to update payment");
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null) return false;

            _context.Payments.Remove(payment);
            return await _unitOfWork.SaveDbChangesAsync();
        }

        public async Task<int> GetTotalCountAsync()
        {
            return await _context.Payments.CountAsync();
        }

        public async Task<int> GetCountByUserIdAsync(int userId)
        {
            return await _context.Payments.CountAsync(p => p.UserId == userId);
        }

        public async Task<int> GetCountByStatusAsync(PaymentStatus status)
        {
            return await _context.Payments.CountAsync(p => p.Status == status);
        }

        public async Task<decimal> GetTotalAmountByUserIdAsync(int userId)
        {
            return await _context.Payments
                .Where(p => p.UserId == userId && p.Status == PaymentStatus.Completed)
                .SumAsync(p => p.Amount);
        }

        public async Task<decimal> GetTotalAmountByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Payments
                .Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate && p.Status == PaymentStatus.Completed)
                .SumAsync(p => p.Amount);
        }

        public async Task<IEnumerable<Payment>> SearchAsync(string searchTerm, int pageNumber, int pageSize)
        {
            var query = _context.Payments
                .Include(p => p.User)
                .Include(p => p.Order)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p =>
                    p.TransactionId.Contains(searchTerm) ||
                    p.PayerEmail.Contains(searchTerm) ||
                    p.PayerName.Contains(searchTerm) ||
                    p.Provider.Contains(searchTerm) ||
                    (p.User != null && (p.User.Email.Contains(searchTerm) || p.User.UserName.Contains(searchTerm))));
            }

            return await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
    }
}
