using Berryfy.Domain.Constants;
using Berryfy.Domain.Entities.ShoppingCartEntities;

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

        public static CartResponse MapFromCart(Cart cart)
        {
            return new CartResponse
            {
                Id = cart.Id,
                UserId = cart.UserId,
                SessionId = cart.SessionId,
                Status = cart.Status,
                ExpiresAt = cart.ExpiresAt,
                Note = cart.Note,
                SubTotal = cart.SubTotal,
                DiscountTotal = cart.DiscountTotal,
                TaxAmount = cart.TaxAmount,
                Total = cart.Total
            };
        }
    }
}
