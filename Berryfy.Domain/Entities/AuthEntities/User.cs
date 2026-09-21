using Berryfy.Domain.Entities.CouponEntities;
using Berryfy.Domain.Entities.OrderEntities;
using Berryfy.Domain.Entities.PaymentEntities;
using Berryfy.Domain.Entities.ShoppingCartEntities;
using Berryfy.Domain.Entities.WishlistEntities;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

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
        public string PhoneNumberComfirmed { get; set; }
        public string TwoFactorEnabled { get; set; }
        public DateTime LockoutEnd { get; set; }
        public bool LockoutEnabled { get; set; }
        public int AccessFailedCount { get; set; }
        public string? EmailConfirmationCode { get; set; }
        public DateTime? EmailConfirmationCodeExpiry { get; set; }

        public string? PasswordResetCode { get; set; }
        public DateTime? PasswordResetCodeExpiry { get; set; }

        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }

        public List<UserCoupon> UserCoupons { get; set; }

        public List<Cart> Cart { get; set; }

        public List<Order> Orders { get; set; }

        public List<UserRole> Roles { get; set; }

        public List<Wishlist> Wishlists { get; set; }

        public List<Payment> Payments { get; set; }
    }
}
