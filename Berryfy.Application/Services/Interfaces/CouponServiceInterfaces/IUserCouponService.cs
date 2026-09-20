using Berryfy.Application.Dtos.AuthDtos.Responses;
using Berryfy.Application.Dtos.CouponDtos.Responses;

namespace Berryfy.Application.Services.Interfaces.CouponServiceInterfaces
{
    public interface IUserCouponService
    {
        Task<UserCouponResponse> AddCouponToUserAsync(int userId, int couponId);
        Task<bool> DisableCouponToUser(int usreId, int couponId);
        Task<List<CouponResponse>> GetCouponsByUserIdAsync(int userId);
        Task<List<UserResponse>> GetUsersByCouponIdAsync(int couponId);
        Task<bool> IsCouponUsedByUser(int userId, string couponCode);
        Task<bool> AddCouponToUsersAsync(List<int> userIds, int couponId);
        Task<bool> AddCouponToAllUsersAsync(int couponId);
        Task<bool> AddCouponToNewUsersAsync(int couponId);
        Task<bool> MarkCouponAsUsedAsync(int userId, int couponId, int orderId);
        Task<bool> RevertCouponUsageAsync(int userId, int couponId, int orderId);
        Task<List<int>> GetCouponIdsUsedInOrderAsync(int orderId);
    }
}
