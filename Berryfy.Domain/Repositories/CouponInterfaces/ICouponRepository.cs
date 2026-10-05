using Berryfy.Domain.Entities;
using Berryfy.Domain.Entities.CouponEntities;
using System.Linq.Expressions;

namespace Berryfy.Domain.Repositories.CouponInterfaces
{
    public interface ICouponRepository
    {
        Task<InfrastructureResponse<Coupon>> GetByIdAsync(int id);
        Task<InfrastructureResponse<Coupon>> GetByCodeAsync(string code);
        Task<InfrastructureResponse<IEnumerable<Coupon>>> GetAllAsync();
        Task<InfrastructureResponse<Coupon>> CreateAsync(Coupon coupon);
        Task<InfrastructureResponse<Coupon>> UpdateAsync(int id, Coupon coupon);
        Task<InfrastructureResponse<bool>> DeleteAsync(Coupon coupon);
        Task<InfrastructureResponse<bool>> ExistsAsync(Expression<Func<Coupon, bool>> expression);
    }
}
