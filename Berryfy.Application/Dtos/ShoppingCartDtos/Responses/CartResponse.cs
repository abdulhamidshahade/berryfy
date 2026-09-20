using Berryfy.Domain.Constants;

namespace Berryfy.Application.Dtos.ShoppingCartDtos.Responses
{
    public class CartResponse
    {
        public int Id { get; set; }
        public bool IsActive { get; set; }
        public int? UserId { get; set; }
        public string? SessionId { get; set; }
        public CartStatus Status { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public List<CartItemResponse> CartItems { get; set; }
        public List<CartCouponResponse> CartCoupons { get; set; } 

        public string? Note { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DiscountTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Total { get; set; }
    }
}
