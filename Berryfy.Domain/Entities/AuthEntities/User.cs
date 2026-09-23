using System.Text.Json.Serialization;
using Berryfy.Domain.Entities.CouponEntities;
using Berryfy.Domain.Entities.OrderEntities;
using Berryfy.Domain.Entities.PaymentEntities;
using Berryfy.Domain.Entities.ShoppingCartEntities;
using Berryfy.Domain.Entities.WishlistEntities;

namespace Berryfy.Domain.Entities.AuthEntities
{
    public class User
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public string NormalizedUserName { get; set; }
        public string NormalizedEmail { get; set; }
        public string? PhoneNumber { get; set; }
        public bool EmailConfirmed { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string SecurityStamp { get; set; }
        public string ConcurrencyStamp { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public bool LockoutEnabled { get; set; }
        public int AccessFailedCount { get; set; }
        public string? EmailConfirmationCode { get; set; }
        public DateTime? EmailConfirmationCodeExpiry { get; set; }
        public string? PasswordResetCode { get; set; }
        public DateTime? PasswordResetCodeExpiry { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }
        [JsonIgnore] public List<UserCoupon> UserCoupons { get; set; }
        [JsonIgnore] public List<Cart> Cart { get; set; } 
        [JsonIgnore] public List<Order> Orders { get; set; }
        [JsonIgnore] public List<UserRole> Roles { get; set; } 
        [JsonIgnore] public List<Wishlist> Wishlists { get; set; }
        [JsonIgnore] public List<Payment> Payments { get; set; }
    }
}
