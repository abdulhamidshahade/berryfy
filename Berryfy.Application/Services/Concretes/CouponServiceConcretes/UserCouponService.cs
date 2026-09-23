using AutoMapper;
using Berryfy.Application.Dtos.AuthDtos.Responses;
using Berryfy.Application.Dtos.CouponDtos.Responses;
using Berryfy.Application.Services.Interfaces.AuthServiceInterfaces;
using Berryfy.Application.Services.Interfaces.CouponServiceInterfaces;
using Berryfy.Domain.Repositories.CouponInterfaces;
using Berryfy.Domain.Repositories.OrderInterfaces;

namespace Berryfy.Application.Services.Concretes.CouponServiceConcretes
{
    public class UserCouponService : IUserCouponService
    {
        private readonly IUserService _userService;
        private readonly ICouponService _couponService;
        private readonly IUserCouponRepository _userCouponRepository;
        private readonly IOrderRepository _orderRepository;


        public UserCouponService(IUserService userService, 
            ICouponService couponService,
            IUserCouponRepository userCouponRepository,
            IOrderRepository orderRepository)
        {
            _userService = userService;
            _couponService = couponService;
            _userCouponRepository = userCouponRepository;
            _orderRepository = orderRepository;
        }


        public async Task<bool> AddCouponToAllUsersAsync(int couponId)
        {
            if (couponId <= 0)
            {
                return false;
            }

            var users = await _userService.GetAllUsers();
            List<int> allUserIds = users.Select(i => i.Id).ToList();

            foreach (var userId in allUserIds)
            {
                var addedCouponToUser = await AddCouponToUserAsync(userId, couponId);

                if (addedCouponToUser == null)
                {
                    return false;
                }
            }

            return true;
        }

        public async Task<bool> AddCouponToNewUsersAsync(int couponId)
        {
            if (couponId <= 0 || !await _couponService.ExistsByIdAsync(couponId)) return false;
            var users = await _userService.GetAllUsers();
            foreach (var userId in users.Select(u => u.Id).Distinct())
            {
                if (await _orderRepository.UserHasPaidOrderAsync(userId)) continue;
                var addedCoupon = await AddCouponToUserAsync(userId, couponId);

                if (addedCoupon == null)
                {
                    return false;
                }
            }

            return true;
        }

        public async Task<UserCouponResponse> AddCouponToUserAsync(int userId, int couponId)
        {
            if(userId <= 0 || couponId <= 0)
            {
                return null;
            }

            var userExists = await _userService.IsUserExistsByIdAsync(userId);
            var couponExists = await _couponService.ExistsByIdAsync(couponId);

            if (!userExists || !couponExists)
            {
                return null;
            }

            var addedCouponToUser = await _userCouponRepository.AddCouponToUserAsync(userId, couponId);
            var userCouponDto = UserCouponResponse.MapFromUserCoupon(addedCouponToUser);
            return userCouponDto;
        }

        public async Task<bool> AddCouponToUsersAsync(List<int> userIds, int couponId)
        {
            foreach( var userId in userIds)
            {
                var addedCoupon = await AddCouponToUserAsync(userId, couponId);

                if(addedCoupon == null)
                {
                    return false;
                }
            }

            return true;
        }

        public async Task<bool> DisableCouponToUser(int userId, int couponId)
        {
            var userExists = await _userService.IsUserExistsByIdAsync(userId);
            var couponExists = await _couponService.GetByIdAsync(couponId);

            if (!userExists || couponExists == null)
            {
                return false;
            }

            List<CouponResponse> userHasCoupons = CouponResponse.MapFromCoupon(
                (await _userCouponRepository.GetCouponsByUserIdAsync(userId)));

            if (!userHasCoupons.Any(i => i.Code == couponExists.Code))
            {
                return false;
            }

            var disabledCoupon = await _userCouponRepository.DisableCouponForUserAsync(userId, couponId);

            return disabledCoupon;
        }

        public async Task<List<CouponResponse>> GetCouponsByUserIdAsync(int userId)
        {
            if (!await _userService.IsUserExistsByIdAsync(userId))
            {
                return null;
            }

            List<CouponResponse> coupons = 
                CouponResponse.MapFromCoupon(await _userCouponRepository.GetCouponsByUserIdAsync(userId));

            if(coupons == null)
            {
                return null;
            }

            return coupons.ToList();
        }

        public async Task<List<UserResponse>> GetUsersByCouponIdAsync(int couponId)
        {
            if (!await _couponService.ExistsByIdAsync(couponId))
            {
                return null;
            }

            List<UserResponse> userList = 
                UserResponse.MapFromUser(await _userCouponRepository.GetUsersByCouponIdAsync(couponId));

            return userList.ToList();
        }

        public async Task<bool> IsCouponUsedByUser(int userId, string couponCode)
        {
            var userExists = await _userService.IsUserExistsByIdAsync(userId);
            var couponExists = await _couponService.ExistsByCodeAsync(couponCode);

            if (!userExists || !couponExists)
            {
                return false;
            }

            var isCouponUsed = await _userCouponRepository.IsCouponUsedByUserAsync(userId, couponCode);

            return isCouponUsed;
        }

        public async Task<bool> MarkCouponAsUsedAsync(int userId, int couponId, int orderId)
        {
            if (userId <= 0 || couponId <= 0 || orderId <= 0)
            {
                return false;
            }

            var userExists = await _userService.IsUserExistsByIdAsync(userId);
            var couponExists = await _couponService.ExistsByIdAsync(couponId);

            if (!userExists || !couponExists)
            {
                return false;
            }

            return await _userCouponRepository.MarkCouponAsUsedAsync(userId, couponId, orderId);
        }

        public async Task<bool> RevertCouponUsageAsync(int userId, int couponId, int orderId)
        {
            if (userId <= 0 || couponId <= 0 || orderId <= 0)
            {
                return false;
            }

            var userExists = await _userService.IsUserExistsByIdAsync(userId);
            var couponExists = await _couponService.ExistsByIdAsync(couponId);

            if (!userExists || !couponExists)
            {
                return false;
            }

            return await _userCouponRepository.RevertCouponUsageAsync(userId, couponId, orderId);
        }

        public async Task<List<int>> GetCouponIdsUsedInOrderAsync(int orderId)
        {
            if (orderId <= 0)
            {
                return new List<int>();
            }

            return await _userCouponRepository.GetCouponIdsUsedInOrderAsync(orderId);
        }
    }
}
