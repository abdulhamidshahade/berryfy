using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.CouponDtos.Requests;
using Berryfy.Application.Dtos.CouponDtos.Responses;

namespace Berryfy.Application.Services.Interfaces.CouponServiceInterfaces
{
    public interface ICouponService
    {
        Task<ApplicationResponse<CouponResponse>> GetByIdAsync(int id);
        Task<ApplicationResponse<CouponResponse>> GetByCodeAsync(string code);
        Task<ApplicationResponse<IEnumerable<CouponResponse>>> GetAllAsync();
        Task<ApplicationResponse<CouponResponse>> CreateAsync(CreateCoupon couponDto);
        Task<ApplicationResponse<CouponResponse>> UpdateAsync(int id, UpdateCoupon couponDto);
        Task<ApplicationResponse<bool>> DeleteAsync(int id);
        Task<ApplicationResponse<bool>> ExistsByIdAsync(int id);
        Task<ApplicationResponse<bool>> ExistsByCodeAsync(string code);
    }
}
