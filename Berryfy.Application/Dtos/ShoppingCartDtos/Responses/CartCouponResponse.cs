using Berryfy.Domain.Entities.ShoppingCartEntities;

namespace Berryfy.Application.Dtos.ShoppingCartDtos.Responses
{
    public class CartCouponResponse
    {
        public int Id { get; set; }
        public int CouponId { get; set; }
        public int UserId { get; set; }
        public int CartId { get; set; }
        public string Description { get; set; }
        public decimal DiscountAmount { get; set; }
        public DateTime AppliedAt { get; set; }

        public static CartCouponResponse MapFromCartCoupon(CartCoupon cartCoupon)
        {
            return new CartCouponResponse()
            {
                Id = cartCoupon.Id,
                CouponId = cartCoupon.CouponId,
                AppliedAt = cartCoupon.AppliedAt,
                CartId = cartCoupon.CartId,
            };
        }
    }
}
