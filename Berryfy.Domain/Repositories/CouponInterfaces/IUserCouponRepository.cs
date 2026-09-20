using Berryfy.Domain.Entities.AuthEntities;
using Berryfy.Domain.Entities.CouponEntities;
namespace Berryfy.Domain.Repositories.CouponInterfaces
{
    public interface IUserCouponRepository
    {
        Task<UserCoupon> AddCouponToUserAsync(int userId, int couponId);
        Task<bool> DisableCouponForUserAsync(int userId, int couponId);
        Task<IReadOnlyList<Coupon>> GetCouponsByUserIdAsync(int userId);
        Task<IReadOnlyList<User>> GetUsersByCouponIdAsync(int couponId);
        Task<bool> IsCouponUsedByUserAsync(int userId, string couponCode);
        Task<bool> MarkCouponAsUsedAsync(int userId, int couponId, int orderId);
        Task<bool> RevertCouponUsageAsync(int userId, int couponId, int orderId);
        Task<List<int>> GetCouponIdsUsedInOrderAsync(int orderId);
    }
}
