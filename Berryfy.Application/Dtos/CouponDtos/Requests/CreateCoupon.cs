using Berryfy.Domain.Constants;
using Berryfy.Domain.Entities.CouponEntities;

namespace Berryfy.Application.Dtos.CouponDtos.Requests
{
    public class CreateCoupon
    {
        public string Code { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal MinimumOrderAmount { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }

        public CouponType Type { get; set; }
        public decimal Value { get; set; }
        public bool IsForNewUsersOnly { get; set; }

        public static Coupon MapToCoupon(CreateCoupon coupon)
        {
            return new Coupon
            {
                Code = coupon.Code,
                DiscountAmount = coupon.DiscountAmount,
                MinimumOrderAmount = coupon.MinimumOrderAmount,
                Description = coupon.Description,
                IsActive = coupon.IsActive,
                Type = coupon.Type,
                Value = coupon.Value,
                IsForNewUsersOnly = coupon.IsForNewUsersOnly
            };
        }
    }
}
