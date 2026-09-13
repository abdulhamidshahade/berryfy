using Berryfy.Domain.Entities.AuthEntities;
using Berryfy.Domain.Entities.CouponEntities;
using Berryfy.Domain.Repositories;
using Berryfy.Domain.Repositories.CouponInterfaces;
using Berryfy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Berryfy.Infrastructure.Repositories.CouponConcretes
{
    public class UserCouponRepository : IUserCouponRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork _unifOfWork;

        public UserCouponRepository(ApplicationDbContext context, IUnitOfWork unitOfWork)
        {
            _context = context;
            _unifOfWork = unitOfWork;
        }

        public async Task<UserCoupon> AddCouponToUserAsync(int userId, int couponId)
        {
            var existing = await _context.UserCoupons
                .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CouponId == couponId);
            if (existing != null) return existing;

            var userCoupon = new UserCoupon
            {
                UserId = userId,
                CouponId = couponId,
                IsUsed = false,
            };

            await _context.UserCoupons.AddAsync(userCoupon);

            if(await _unifOfWork.SaveDbChangesAsync())
            {
                return userCoupon;
            }

            return null;
        }

        public async Task<UserCoupon> AddUserToCouponAsync(int userId, int couponId)
        {
            return await AddCouponToUserAsync(userId, couponId);
        }

        public async Task<bool> DisableCouponForUserAsync(int userId, int couponId)
        {
            var userCouponModel = await _context.UserCoupons.Where(i => i.UserId == userId && i.CouponId == couponId)
                .FirstOrDefaultAsync();

            if (userCouponModel == null) return false;
            if (userCouponModel.IsUsed) return true;
            userCouponModel.IsUsed = true;
            return await _unifOfWork.SaveDbChangesAsync();
        }

        public async Task<IReadOnlyList<Coupon>> GetCouponsByUserIdAsync(int userId)
        {
            var coupons = await _context.UserCoupons.Where(i => i.UserId == userId)
                .Select(c => c.Coupon)
                .ToListAsync();

            return coupons;
        }

        public async Task<IReadOnlyList<ApplicationUser>> GetUsersByCouponIdAsync(int couponId)
        {
            var users = await _context.UserCoupons.Where(c => c.CouponId == couponId)
                .Select(u => u.User)
                .ToListAsync();

            return users;
        }

        public async Task<bool> IsCouponUsedByUserAsync(int userId, string couponCode)
        {
            var coupon = await _context.UserCoupons.Where(i => i.UserId == userId && i.Coupon.Code == couponCode)
                .FirstOrDefaultAsync();

            if(coupon == null)
            {
                return false;
            }

            return coupon.IsUsed;
        }

        public async Task<bool> MarkCouponAsUsedAsync(int userId, int couponId, int orderId)
        {
            var userCoupon = await _context.UserCoupons
                .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CouponId == couponId);

            if (userCoupon == null)
            {
                return false;
            }

            if (userCoupon.IsUsed) return userCoupon.OrderId == orderId;
            userCoupon.IsUsed = true;
            userCoupon.UsedAt = DateTime.UtcNow;
            userCoupon.OrderId = orderId;

            return await _unifOfWork.SaveDbChangesAsync();
        }

        public async Task<bool> RevertCouponUsageAsync(int userId, int couponId, int orderId)
        {
            var userCoupon = await _context.UserCoupons
                .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CouponId == couponId && uc.OrderId == orderId);

            if (userCoupon == null)
            {
                return false;
            }

            userCoupon.IsUsed = false;
            userCoupon.UsedAt = null;
            userCoupon.OrderId = null;

            return await _unifOfWork.SaveDbChangesAsync();
        }

        public async Task<List<int>> GetCouponIdsUsedInOrderAsync(int orderId)
        {
            return await _context.UserCoupons
                .Where(uc => uc.OrderId == orderId && uc.IsUsed)
                .Select(uc => uc.CouponId)
                .ToListAsync();
        }
    }
}