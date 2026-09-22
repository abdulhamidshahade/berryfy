using System.Text.Json.Serialization;
using Berryfy.Domain.Entities.AuthEntities;
using Berryfy.Domain.Entities.Base;

namespace Berryfy.Domain.Entities.CouponEntities
{
    public class UserCoupon : IAuditableEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        [JsonIgnore] public User User { get; set; }

        public int CouponId { get; set; }
        [JsonIgnore] public Coupon Coupon { get; set; }
        public bool IsUsed { get; set; }
        public DateTime? UsedAt { get; set; }
        public int? OrderId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
