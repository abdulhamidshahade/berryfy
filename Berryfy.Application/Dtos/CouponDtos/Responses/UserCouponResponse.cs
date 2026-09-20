namespace Berryfy.Application.Dtos.CouponDtos.Responses
{
    public class UserCouponResponse
    {
        public int UserId { get; set; }
        public int CouponId { get; set; }
        public bool IsUsed { get; set; } = false;
    }
}
