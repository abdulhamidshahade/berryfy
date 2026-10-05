using Berryfy.Domain.Entities;
using Berryfy.Domain.Entities.AuthEntities;
using Berryfy.Domain.Entities.CouponEntities;
namespace Berryfy.Domain.Repositories.CouponInterfaces
{
    public interface IUserCouponRepository
    {
        Task<InfrastructureResponse<UserCoupon>> AddCouponToUserAsync(int userId, int couponId);
        Task<InfrastructureResponse<bool>> DisableCouponForUserAsync(int userId, int couponId);
        Task<InfrastructureResponse<IReadOnlyList<Coupon>>> GetCouponsByUserIdAsync(int userId);
        Task<InfrastructureResponse<IReadOnlyList<User>>> GetUsersByCouponIdAsync(int couponId);
        Task<InfrastructureResponse<bool>> IsCouponUsedByUserAsync(int userId, string couponCode);
        Task<InfrastructureResponse<bool>> MarkCouponAsUsedAsync(int userId, int couponId, int orderId);
        Task<InfrastructureResponse<bool>> RevertCouponUsageAsync(int userId, int couponId, int orderId);
        Task<InfrastructureResponse<List<int>>> GetCouponIdsUsedInOrderAsync(int orderId);
    }
}
