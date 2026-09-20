using Berryfy.Application.Dtos.CouponDtos.Requests;
using Berryfy.Application.Dtos.CouponDtos.Responses;

namespace Berryfy.Application.Services.Interfaces.CouponServiceInterfaces
{
    public interface ICouponService
    {
        Task<CouponResponse> GetByIdAsync(int id);
        Task<CouponResponse> GetByCodeAsync(string code);
        Task<IEnumerable<CouponResponse>> GetAllAsync();
        Task<CouponResponse> CreateAsync(CreateCoupon couponDto);
        Task<CouponResponse> UpdateAsync(int id, UpdateCoupon couponDto);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsByIdAsync(int id);
        Task<bool> ExistsByCodeAsync(string code);
    }
}
