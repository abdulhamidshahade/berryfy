using Berryfy.Application.Authorization.Attributes;
using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.AuthDtos.Responses;
using Berryfy.Application.Dtos.CouponDtos.Requests;
using Berryfy.Application.Dtos.CouponDtos.Responses;
using Berryfy.Application.Services.Interfaces.CouponServiceInterfaces;
using Microsoft.AspNetCore.Mvc;

namespace Berryfy.API.Controllers
{
    [Route("api/coupons")]
    [ApiController]
    public class CouponsController : BaseController
    {
        private readonly ICouponService _couponService;
        private readonly IUserCouponService _userCouponService;
        public CouponsController(ICouponService couponService,
                                 IUserCouponService userCouponService)
        {
            _couponService = couponService;
            _userCouponService = userCouponService;
        }


        [HttpGet]
        [AdminAndAbove]
        public async Task<ActionResult<ApiResponse<IEnumerable<CouponResponse>>>> GetAll()
        {
            var coupons = await _couponService.GetAllAsync();

            if (coupons == null)
            {
                return new ApiResponse<IEnumerable<CouponResponse>>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    StatusMessage = "Error retrieving coupons",
                    Errors = new List<string> { "An unexpected error occurred" }
                };
            }
            var response = new ApiResponse<IEnumerable<CouponResponse>>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                StatusMessage = "Coupons retrieved successfully",
                Data = coupons
            };
            return Ok(response);
        }


        [HttpGet]
        [Route("{id}")]
        [UserAndAbove]
        public async Task<ActionResult<ApiResponse<CouponResponse>>> GetById(int id)
        {
            var coupon = await _couponService.GetByIdAsync(id);

            if (coupon == null)
            {
                return new ApiResponse<CouponResponse>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    StatusMessage = "Error retrieving coupon",
                    Errors = new List<string> { "An unexpected error occurred" }
                };

            }
            var response = new ApiResponse<CouponResponse>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                StatusMessage = "Coupon retrieved successfully",
                Data = coupon
            };
            return Ok(response);
        }


        [HttpGet]
        [Route("code/{code}")]
        [UserAndAbove]
        public async Task<ActionResult<ApiResponse<CouponResponse>>> GetByCode(string code)
        {
            var coupon = await _couponService.GetByCodeAsync(code);

            if (coupon == null)
            {
                return new ApiResponse<CouponResponse>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    StatusMessage = "Error retrieving coupon",
                    Errors = new List<string> { "An unexpected error occurred" }
                };

            }
            var response = new ApiResponse<CouponResponse>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                StatusMessage = "Coupon retrieved successfully",
                Data = coupon
            };
            return Ok(response);
        }


        [HttpPost]
        [AdminAndAbove]
        public async Task<ActionResult<ApiResponse<CouponResponse>>> Create([FromBody] CreateCoupon couponDto)
        {
            var createdCoupon = await _couponService.CreateAsync(couponDto);

            if (createdCoupon == null)
            {
                return new ApiResponse<CouponResponse>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    StatusMessage = "Error creating coupon",
                    Errors = new List<string> { "An unexpected error occurred" }
                };

            }
            var response = new ApiResponse<CouponResponse>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status201Created,
                StatusMessage = "Coupon created successfully",
                Data = createdCoupon
            };
            return response;


        }


        [HttpPut]
        [Route("{id}")]
        [AdminAndAbove]
        public async Task<ActionResult<ApiResponse<CouponResponse>>> Update(int id, [FromBody] UpdateCoupon couponDto)
        {
            var updatedCoupon = await _couponService.UpdateAsync(id, couponDto);

            if (updatedCoupon == null)
            {
                return new ApiResponse<CouponResponse>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    StatusMessage = "Error updating coupon",
                    Errors = new List<string> { "An unexpected error occurred" }
                };

            }
            var response = new ApiResponse<CouponResponse>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                StatusMessage = "Coupon updated successfully",
                Data = updatedCoupon
            };
            return Ok(response);
        }


        [HttpDelete]
        [Route("{id}")]
        [AdminAndAbove]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
        {
            var deleted = await _couponService.DeleteAsync(id);

            if (!deleted)
            {
                var notFoundResponse = new ApiResponse<bool>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    StatusMessage = "Coupon not found",
                    Errors = new List<string> { $"Coupon with ID {id} not found" },
                    Data = false
                };
                return NotFound(notFoundResponse);
            }

            var response = new ApiResponse<bool>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                StatusMessage = "Coupon deleted successfully",
                Data = true
            };
            return Ok(response);
        }


        [HttpGet]
        [Route("exists-by-id/{id}")]
        [UserAndAbove]
        public async Task<ActionResult<ApiResponse<bool>>> Exists(int id)
        {
            var exists = await _couponService.ExistsByIdAsync(id);

            if (exists)
            {
                var response = new ApiResponse<bool>
                {
                    IsSuccess = true,
                    StatusCode = StatusCodes.Status200OK,
                    StatusMessage = "Coupon exists",
                    Data = true
                };
                return Ok(response);
            }

            var notFoundResponse = new ApiResponse<bool>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status404NotFound,
                StatusMessage = "Coupon not found",
                Data = false
            };
            return NotFound(notFoundResponse);
        }


        [HttpGet]
        [Route("exists-by-code/{code}")]
        [UserAndAbove]
        public async Task<ActionResult<ApiResponse<bool>>> ExistsByCode(string code)
        {
            var exists = await _couponService.ExistsByCodeAsync(code);

            if (exists)
            {
                var response = new ApiResponse<bool>
                {
                    IsSuccess = true,
                    StatusCode = StatusCodes.Status200OK,
                    StatusMessage = "Coupon exists",
                    Data = true
                };
                return Ok(response);
            }

            var notFoundResponse = new ApiResponse<bool>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status404NotFound,
                StatusMessage = "Coupon not found",
                Data = false
            };
            return NotFound(notFoundResponse);
        }


        [HttpPost]
        [Route("users/{userId}/coupons")]
        [AdminAndAbove]
        public async Task<ActionResult<ApiResponse<UserCouponResponse>>> AddCouponToUser(int userId, [FromBody] AddCouponToUser addCouponToUser)
        {
            var entity = await _userCouponService.AddCouponToUserAsync(userId, addCouponToUser.CouponId);

            if (entity != null)
            {
                return Ok(new ApiResponse<UserCouponResponse>
                {
                    Data = entity,
                    IsSuccess = true,
                    StatusCode = 201
                });
            }

            return BadRequest(new ApiResponse<UserCouponResponse>
            {
                IsSuccess = false,
                StatusCode = 400,
                StatusMessage = "An error occurred while apply coupon to user"
            });
        }

        [HttpPut]
        [Route("users/{userId}/coupons/{couponId}/disable")]
        [AdminAndAbove]
        public async Task<ActionResult<ApiResponse<bool>>> DisableUserCoupon(int userId, int couponId)
        {
            var isDisabled = await _userCouponService.DisableCouponToUser(userId, couponId);

            if (isDisabled)
            {
                return Ok(new ApiResponse<bool>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = $"The coupon has disabled for the user: {userId}",
                    Data = true
                });
            }

            return BadRequest(new ApiResponse<bool>
            {
                IsSuccess = false,
                StatusCode = 400,
                StatusMessage = "En error occurred while disabling the coupon",
                Data = false
            });
        }


        [HttpGet]
        [Route("users/{userId}/coupons")]
        [UserAndAbove]
        public async Task<ActionResult<ApiResponse<List<CouponResponse>>>> GetCouponsByUserId(int userId)
        {
            var coupons = await _userCouponService.GetCouponsByUserIdAsync(userId);

            if (coupons.Count != 0)
            {
                return Ok(new ApiResponse<List<CouponResponse>>
                {
                    Data = coupons,
                    IsSuccess = true,
                    StatusCode = 200
                });
            }

            return NotFound(new ApiResponse<List<CouponResponse>>
            {
                IsSuccess = false,
                StatusCode = 404,
                StatusMessage = "There is no coupons found by user"
            });
        }


        [HttpGet]
        [Route("{couponId}/users")]
        [AdminAndAbove]
        public async Task<ActionResult<ApiResponse<List<UserResponse>>>> GetUsersByCouponId(int couponId)
        {
            var users = await _userCouponService.GetUsersByCouponIdAsync(couponId);

            if (users.Count != 0)
            {
                return Ok(new ApiResponse<List<UserResponse>>
                {
                    Data = users,
                    IsSuccess = true,
                    StatusCode = 200
                });
            }

            return NotFound(new ApiResponse<List<UserResponse>>
            {
                IsSuccess = false,
                StatusCode = 404,
                StatusMessage = "There is no users found by coupon"
            });
        }


        [HttpGet]
        [Route("users/{userId}/coupons/{couponCode}/used")]
        [UserAndAbove]
        public async Task<ActionResult<ApiResponse<bool>>> HasUserUsedCoupon(int userId, string couponCode)
        {

            var hasUsed = await _userCouponService.IsCouponUsedByUser(userId, couponCode);

            if (hasUsed)
            {
                return Ok(new ApiResponse<bool>
                {
                    StatusCode = 200,
                    IsSuccess = true,
                    StatusMessage = "The coupon has used",
                    Data = true,
                });
            }
            else if (!hasUsed)
            {
                return Ok(new ApiResponse<bool>
                {
                    StatusMessage = "The coupon has not used",
                    StatusCode = 200,
                    IsSuccess = true,
                    Data = false
                });
            }

            else
            {
                return BadRequest(new ApiResponse<bool>
                {
                    StatusMessage = "Error exists while checking",
                    StatusCode = 400,
                    IsSuccess = false,
                    Data = false
                });
            }
        }


        [HttpPost]
        [Route("add-coupon-to-specific-users/{couponId}")]
        [AdminAndAbove]
        public async Task<ActionResult<ApiResponse<bool>>> AddCouponToSpecificUsers(int couponId, [FromQuery]List<int> UserIds)
        {
            var addedCoupon = await _userCouponService.AddCouponToUsersAsync(UserIds, couponId);

            if (!addedCoupon)
            {
                return BadRequest(new ApiResponse<bool>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = "An error occurred while applying coupon to users",
                    Data= false,
                });
            }

            return Ok(new ApiResponse<bool>
            {
                IsSuccess = true,
                StatusCode = 200,
                StatusMessage = "Coupon added to specific users successfully",
                Data = true
            });
        }

        [HttpPost]
        [Route("add-coupon-to-all-users/{couponId}")]
        [AdminAndAbove]
        public async Task<ActionResult<ApiResponse<bool>>> AddCouponToAllUsers(int couponId)
        {
            var addedCoupon = await _userCouponService.AddCouponToAllUsersAsync(couponId);

            if (!addedCoupon)
            {
                return BadRequest(new ApiResponse<bool>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = "An error occurred while applying coupon to all users",
                    Data = false
                });
            }

            return Ok(new ApiResponse<bool>
            {
                IsSuccess = true,
                StatusCode = 200,
                StatusMessage = "Coupon added to all users successfully",
                Data = true
            });
        }

        [HttpPost]
        [Route("add-coupon-to-new-users/{couponId}")]
        [AdminAndAbove]
        public async Task<ActionResult<ApiResponse<bool>>> AddCouponToNewUsers(int couponId)
        {
            var addedCoupon = await _userCouponService.AddCouponToNewUsersAsync(couponId);

            if (!addedCoupon)
            {
                return BadRequest(new ApiResponse<bool>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = "An error occurred while applying coupon to new users",
                    Data = false
                });
            }

            return Ok(new ApiResponse<bool>
            {
                IsSuccess = true,
                StatusCode = 200,
                StatusMessage = "Coupon added to new users successfully",
                Data = true
            });
        }
    }
}