using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.AuthDtos.Responses;
using Berryfy.Application.Dtos.CouponDtos.Responses;

namespace Berryfy.Application.Services.Interfaces.CouponServiceInterfaces
{
    public interface IUserCouponService
    {
        Task<ApplicationResponse<UserCouponResponse>> AddCouponToUserAsync(int userId, int couponId);
        Task<ApplicationResponse<bool>> DisableCouponToUser(int usreId, int couponId);
        Task<ApplicationResponse<List<CouponResponse>>> GetCouponsByUserIdAsync(int userId);
        Task<ApplicationResponse<List<UserResponse>>> GetUsersByCouponIdAsync(int couponId);
        Task<ApplicationResponse<bool>> IsCouponUsedByUser(int userId, string couponCode);
        Task<ApplicationResponse<bool>> AddCouponToUsersAsync(List<int> userIds, int couponId);
        Task<ApplicationResponse<bool>> AddCouponToAllUsersAsync(int couponId);
        Task<ApplicationResponse<bool>> AddCouponToNewUsersAsync(int couponId);
        Task<ApplicationResponse<bool>> MarkCouponAsUsedAsync(int userId, int couponId, int orderId);
        Task<ApplicationResponse<bool>> RevertCouponUsageAsync(int userId, int couponId, int orderId);
        Task<ApplicationResponse<List<int>>> GetCouponIdsUsedInOrderAsync(int orderId);
    }
}
