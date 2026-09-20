using Berryfy.Domain.Constants;
using Berryfy.Domain.Entities.CouponEntities;

namespace Berryfy.Application.Dtos.CouponDtos.Responses
{
    public class CouponResponse
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal MinimumOrderAmount { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }

        public CouponType Type { get; set; }
        public decimal Value { get; set; }
        public bool IsForNewUsersOnly { get; set; }

        public static CouponResponse MapFromCoupon(Coupon coupon)
        {
            return new CouponResponse
            {
                Id = coupon.Id,
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

        public static Coupon MapToCoupon(CouponResponse couponResponse)
        {
            return new Coupon
            {
                Id = couponResponse.Id,
                Code = couponResponse.Code,
                DiscountAmount = couponResponse.DiscountAmount,
                MinimumOrderAmount = couponResponse.MinimumOrderAmount,
                Description = couponResponse.Description,
                IsActive = couponResponse.IsActive,
                Type = couponResponse.Type,
                Value = couponResponse.Value,
                IsForNewUsersOnly = couponResponse.IsForNewUsersOnly
            };
        }

        public static List<Coupon> MapToCoupon(IEnumerable<CouponResponse> couponResponses)
        {
            List<Coupon> coupons = new List<Coupon>();

            foreach(CouponResponse couponResponse in couponResponses)
            {
                coupons.Add(MapToCoupon(couponResponse));
            }

            return coupons;
        }

        public static List<CouponResponse> MapFromCoupon(IEnumerable<Coupon> coupons)
        {
            List<CouponResponse> couponResponses = new List<CouponResponse>();

            foreach(Coupon coupon in coupons)
            {
                couponResponses.Add(MapFromCoupon(coupon));
            }

            return couponResponses;
        }
    }
}
