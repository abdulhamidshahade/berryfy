using Berryfy.Domain.Constants;
using Berryfy.Domain.Entities.CouponEntities;

namespace Berryfy.Application.Dtos.CouponDtos.Requests
{
    public class UpdateCoupon
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

        public static Coupon MapToCoupon(UpdateCoupon request)
        {
            return new Coupon
            {
                Id = request.Id,
                Code = request.Code,
                DiscountAmount = request.DiscountAmount,
                MinimumOrderAmount = request.MinimumOrderAmount,
                Description = request.Description,
                IsActive = request.IsActive,
                Type = request.Type,
                Value = request.Value,
                IsForNewUsersOnly = request.IsForNewUsersOnly
            };
        }
    }
}
