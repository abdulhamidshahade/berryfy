using Berryfy.Domain.Entities.CouponEntities;

namespace Berryfy.Application.Dtos.CouponDtos.Responses
{
    public class UserCouponResponse
    {
        public int UserId { get; set; }
        public int CouponId { get; set; }
        public bool IsUsed { get; set; } = false;

        public static UserCouponResponse MapFromUserCoupon(UserCoupon userCoupon)
        {
            return new UserCouponResponse
            {
                UserId = userCoupon.Id,
                CouponId = userCoupon.CouponId,
                IsUsed = userCoupon.IsUsed
            };
        }
    }
}
