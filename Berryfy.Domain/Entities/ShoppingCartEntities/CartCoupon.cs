using System.Text.Json.Serialization;
using Berryfy.Domain.Entities.AuthEntities;
using Berryfy.Domain.Entities.Base;
using Berryfy.Domain.Entities.CouponEntities;

namespace Berryfy.Domain.Entities.ShoppingCartEntities
{
    public class CartCoupon : IAuditableEntity
    {
        public int Id { get; set; }


        public int CartId { get; set; }
        public int CouponId { get; set; }


        public int? UserId { get; set; }
        public string? SessionId { get; set; }


        public decimal DiscountAmount { get; set; }


        public DateTime AppliedAt { get; set; }


        [JsonIgnore] public virtual Cart Cart { get; set; } = null!;
        [JsonIgnore] public virtual Coupon Coupon { get; set; } = null!;
        [JsonIgnore] public virtual User? User { get; set; }




        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
