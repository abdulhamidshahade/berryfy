using AutoMapper;
using Berryfy.Application.Dtos;
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


        public async Task<ApplicationResponse<bool>> AddCouponToAllUsersAsync(int couponId)
        {
            if (couponId <= 0)
            {
                return new ApplicationResponse<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid coupon ID"
                };
            }

            var users = _userService.GetAllUsers().GetAwaiter().GetResult().Value;
            List<int> allUserIds = users.Select(i => i.Id).ToList();

            foreach (var userId in allUserIds)
            {
                var addedCouponToUser = await AddCouponToUserAsync(userId, couponId);

                if (addedCouponToUser == null)
                {
                    return new ApplicationResponse<bool>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Failed to add coupon to user"
                    };
                }
            }

            return new ApplicationResponse<bool>
            {
                IsSuccess = true,
                SuccessMessage = "Coupon added to all users successfully"
            };
        }

        public async Task<ApplicationResponse<bool>> AddCouponToNewUsersAsync(int couponId)
        {
            if (couponId <= 0 || ! _couponService.ExistsByIdAsync(couponId).GetAwaiter().GetResult().Value) return new ApplicationResponse<bool>
            {
                IsSuccess = false,
                ErrorMessage = "Invalid coupon ID"
            };
            var users = _userService.GetAllUsers().GetAwaiter().GetResult().Value;
            foreach (var userId in users.Select(u => u.Id).Distinct())
            {
                if (_orderRepository.UserHasPaidOrderAsync(userId).GetAwaiter().GetResult().Value) continue;
                var addedCoupon = await AddCouponToUserAsync(userId, couponId);

                if (addedCoupon == null)
                {
                    return new ApplicationResponse<bool>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Failed to add coupon to user"
                    };
                }
            }

            return new ApplicationResponse<bool>
            {
                IsSuccess = true,
                SuccessMessage = "Coupon added to new users successfully"
            };
        }

        public async Task<ApplicationResponse<UserCouponResponse>> AddCouponToUserAsync(int userId, int couponId)
        {
            if(userId <= 0 || couponId <= 0)
            {
                return new ApplicationResponse<UserCouponResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid user or coupon ID"
                };
            }

            var userExists = _userService.IsUserExistsByIdAsync(userId).GetAwaiter().GetResult().Value;
            var couponExists = _couponService.ExistsByIdAsync(couponId).GetAwaiter().GetResult().Value;

            if (!userExists || !couponExists)
            {
                return new ApplicationResponse<UserCouponResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "User or coupon not found"
                };
            }

            var addedCouponToUser = _userCouponRepository.AddCouponToUserAsync(userId, couponId).GetAwaiter().GetResult().Value;
            var userCouponDto = UserCouponResponse.MapFromUserCoupon(addedCouponToUser);
            return new ApplicationResponse<UserCouponResponse>
            {
                IsSuccess = true,
                SuccessMessage = "Coupon added to user successfully",
                Value = userCouponDto
            };
        }

        public async Task<ApplicationResponse<bool>> AddCouponToUsersAsync(List<int> userIds, int couponId)
        {
            foreach( var userId in userIds)
            {
                var addedCoupon = await AddCouponToUserAsync(userId, couponId);

                if(addedCoupon == null)
                {
                    return new ApplicationResponse<bool>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Failed to add coupon to user"
                    };
                }
            }

            return new ApplicationResponse<bool>
            {
                IsSuccess = true,
                SuccessMessage = "Coupon added to users successfully"
            };
        }

        public async Task<ApplicationResponse<bool>> DisableCouponToUser(int userId, int couponId)
        {
            var userExists = _userService.IsUserExistsByIdAsync(userId).GetAwaiter().GetResult().Value;
            var couponExists = _couponService.GetByIdAsync(couponId).GetAwaiter().GetResult().Value;

            if (!userExists || couponExists == null)
            {
                return new ApplicationResponse<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = "User or coupon not found"
                };
            }

            List<CouponResponse> userHasCoupons = CouponResponse.MapFromCoupon(
                (_userCouponRepository.GetCouponsByUserIdAsync(userId).GetAwaiter().GetResult().Value));

            if (!userHasCoupons.Any(i => i.Code == couponExists.Code))
            {
                return new ApplicationResponse<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = "Coupon not found for user"
                };
            }

            var disabledCoupon = await _userCouponRepository.DisableCouponForUserAsync(userId, couponId);

            return new ApplicationResponse<bool>
            {
                IsSuccess = true,
                SuccessMessage = "Coupon disabled for user successfully"
            };
        }

        public async Task<ApplicationResponse<List<CouponResponse>>> GetCouponsByUserIdAsync(int userId)
        {
            if (_userService.IsUserExistsByIdAsync(userId).GetAwaiter().GetResult().Value)
            {
                return new ApplicationResponse<List<CouponResponse>>
                {
                    IsSuccess = false,
                    ErrorMessage = "User not found"
                };
            }

            List<CouponResponse> coupons = 
                CouponResponse.MapFromCoupon(_userCouponRepository.GetCouponsByUserIdAsync(userId).GetAwaiter().GetResult().Value);

            if(coupons == null)
            {
                return new ApplicationResponse<List<CouponResponse>>
                {
                    IsSuccess = false,
                    ErrorMessage = "No coupons found for user"
                };
            }

            return new ApplicationResponse<List<CouponResponse>>
            {
                IsSuccess = true,
                SuccessMessage = "Coupons retrieved successfully",
                Value = coupons.ToList()
            };
        }

        public async Task<ApplicationResponse<List<UserResponse>>> GetUsersByCouponIdAsync(int couponId)
        {
            if (!_couponService.ExistsByIdAsync(couponId).GetAwaiter().GetResult().Value)
            {
                return new ApplicationResponse<List<UserResponse>>
                {
                    IsSuccess = false,
                    ErrorMessage = "Coupon not found"
                };
            }

            List<UserResponse> userList = 
                UserResponse.MapFromUser(_userCouponRepository.GetUsersByCouponIdAsync(couponId).GetAwaiter().GetResult().Value);

            return new ApplicationResponse<List<UserResponse>>
            {
                IsSuccess = true,
                SuccessMessage = "Users retrieved successfully",
                Value = userList.ToList()
            };
        }

        public async Task<ApplicationResponse<bool>> IsCouponUsedByUser(int userId, string couponCode)
        {
            var userExists =  _userService.IsUserExistsByIdAsync(userId).GetAwaiter().GetResult().Value;
            var couponExists = _couponService.ExistsByCodeAsync(couponCode).GetAwaiter().GetResult().Value;

            if (!userExists || !couponExists)
            {
                return new ApplicationResponse<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = "User or coupon not found"
                };
            }

            var isCouponUsed = _userCouponRepository.IsCouponUsedByUserAsync(userId, couponCode).GetAwaiter().GetResult().Value;

            return new ApplicationResponse<bool>
            {
                IsSuccess = true,
                SuccessMessage = "Coupon usage checked successfully",
                Value = isCouponUsed
            };
        }

        public async Task<ApplicationResponse<bool>> MarkCouponAsUsedAsync(int userId, int couponId, int orderId)
        {
            if (userId <= 0 || couponId <= 0 || orderId <= 0)
            {
                return new ApplicationResponse<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid user, coupon, or order ID"
                };
            }

            var userExists = _userService.IsUserExistsByIdAsync(userId).GetAwaiter().GetResult().Value;
            var couponExists = _couponService.ExistsByIdAsync(couponId).GetAwaiter().GetResult().Value;

            if (!userExists || !couponExists)
            {
                return new ApplicationResponse<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = "User or coupon not found"
                };
            }

            return new ApplicationResponse<bool>
            {
                IsSuccess = true,
                SuccessMessage = "Coupon marked as used successfully",
                Value = _userCouponRepository.MarkCouponAsUsedAsync(userId, couponId, orderId).GetAwaiter().GetResult().Value
            };
        }

        public async Task<ApplicationResponse<bool>> RevertCouponUsageAsync(int userId, int couponId, int orderId)
        {
            if (userId <= 0 || couponId <= 0 || orderId <= 0)
            {
                return new ApplicationResponse<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid user, coupon, or order ID"
                };
            }

            var userExists = _userService.IsUserExistsByIdAsync(userId).GetAwaiter().GetResult().Value;
            var couponExists = _couponService.ExistsByIdAsync(couponId).GetAwaiter().GetResult().Value;

            if (!userExists || !couponExists)
            {
                return new ApplicationResponse<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = "User or coupon not found"
                };
            }

            return new ApplicationResponse<bool>
            {
                IsSuccess = true,
                SuccessMessage = "Coupon usage reverted successfully",
                Value = _userCouponRepository.RevertCouponUsageAsync(userId, couponId, orderId).GetAwaiter().GetResult().Value
            };
        }

        public async Task<ApplicationResponse<List<int>>> GetCouponIdsUsedInOrderAsync(int orderId)
        {
            if (orderId <= 0)
            {
                return new ApplicationResponse<List<int>>
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid order ID"
                };
            }

            return new ApplicationResponse<List<int>>
            {
                IsSuccess = true,
                SuccessMessage = "Coupon IDs retrieved successfully",
                Value = _userCouponRepository.GetCouponIdsUsedInOrderAsync(orderId).GetAwaiter().GetResult().Value
            };
        }
    }
}