using Berryfy.Domain.Constants;

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
    }
}
