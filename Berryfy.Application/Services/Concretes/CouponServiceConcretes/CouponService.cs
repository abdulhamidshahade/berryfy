using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.CouponDtos.Requests;
using Berryfy.Application.Dtos.CouponDtos.Responses;
using Berryfy.Application.Services.Interfaces.CouponServiceInterfaces;

using Berryfy.Domain.Repositories.CouponInterfaces;


namespace Berryfy.Application.Services.Concretes.CouponServiceConcretes
{
    public class CouponService : ICouponService
    {
        private readonly ICouponRepository _couponRepository;

        public CouponService(ICouponRepository couponRepository)
        {
            _couponRepository = couponRepository;
        }

        public async Task<ApplicationResponse<CouponResponse?>> GetByIdAsync(int id)
        {
            if(id <= 0)
            {
                return new ApplicationResponse<CouponResponse?>
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid coupon ID"
                };
            }

            var coupon = _couponRepository.GetByIdAsync(id).GetAwaiter().GetResult().Value;

            if (coupon == null)
            {
                return new ApplicationResponse<CouponResponse?>
                {
                    IsSuccess = false,
                    ErrorMessage = "Coupon not found"
                };
            }

            return new ApplicationResponse<CouponResponse?>
            {
                IsSuccess = true,
                SuccessMessage = "Coupon retrieved successfully",
                Value = CouponResponse.MapFromCoupon(coupon)
            };
        }

        public async Task<ApplicationResponse<CouponResponse?>> GetByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return new ApplicationResponse<CouponResponse?>
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid coupon code"
                };
            }

            var coupon = _couponRepository.GetByCodeAsync(code).GetAwaiter().GetResult().Value;
            if (coupon == null)
            {
                return new ApplicationResponse<CouponResponse?>
                {
                    IsSuccess = false,
                    ErrorMessage = "Coupon not found"
                };
            }

            return new ApplicationResponse<CouponResponse?>
            {
                IsSuccess = true,
                SuccessMessage = "Coupon retrieved successfully",
                Value = CouponResponse.MapFromCoupon(coupon)
            };
        }

        public async Task<ApplicationResponse<IEnumerable<CouponResponse>>> GetAllAsync()
        {
            var coupons = _couponRepository.GetAllAsync().GetAwaiter().GetResult().Value;
            return new ApplicationResponse<IEnumerable<CouponResponse>>
            {
                IsSuccess = true,
                SuccessMessage = "Coupons retrieved successfully",
                Value = CouponResponse.MapFromCoupon(coupons)
            };
        }

        public async Task<ApplicationResponse<CouponResponse>> CreateAsync(CreateCoupon request)
        {
            if (request == null)
            {
                return new ApplicationResponse<CouponResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid coupon request"
                };
            }

            if (ExistsByCodeAsync(request.Code).GetAwaiter().GetResult().Value)
            {
                return new ApplicationResponse<CouponResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Coupon with this code already exists"
                };
            }

            var coupon = CreateCoupon.MapToCoupon(request);
            var createdCoupon = _couponRepository.CreateAsync(coupon).GetAwaiter().GetResult().Value;

            return new ApplicationResponse<CouponResponse>
            {
                IsSuccess = true,
                SuccessMessage = "Coupon created successfully",
                Value = CouponResponse.MapFromCoupon(createdCoupon)
            };
        }

        public async Task<ApplicationResponse<CouponResponse>> UpdateAsync(int id, UpdateCoupon request)
        {
            if (request == null)
            {
                return new ApplicationResponse<CouponResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid coupon request"
                };
            }

            var existingCoupon = _couponRepository.GetByIdAsync(id).GetAwaiter().GetResult().Value;

            if (existingCoupon == null)
            {
                return new ApplicationResponse<CouponResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Coupon not found"
                };
            }

            var isCouponExists = _couponRepository.GetByCodeAsync(request.Code).GetAwaiter().GetResult().Value;

            if(isCouponExists != null && isCouponExists.Id != request.Id)
            {
                return new ApplicationResponse<CouponResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Coupon with this code already exists"
                };
            }

            var mappedCoupon = UpdateCoupon.MapToCoupon(request);

            var updatedCoupon = _couponRepository.UpdateAsync(id, mappedCoupon).GetAwaiter().GetResult().Value;
            return new ApplicationResponse<CouponResponse>
            {
                IsSuccess = true,
                SuccessMessage = "Coupon updated successfully",
                Value = CouponResponse.MapFromCoupon(updatedCoupon)
            };
        }

        public async Task<ApplicationResponse<bool>> DeleteAsync(int id)
        {
            if(id <= 0)
            {
                return new ApplicationResponse<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid coupon ID"
                };
            }

            var coupon = _couponRepository.GetByIdAsync(id).GetAwaiter().GetResult().Value;
            if (coupon == null)
            {
                return new ApplicationResponse<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = "Coupon not found"
                };
            }

            var deletedCoupon = _couponRepository.DeleteAsync(coupon).GetAwaiter().GetResult().Value;
            return new ApplicationResponse<bool>
            {
                IsSuccess = true,
                SuccessMessage = "Coupon deleted successfully",
                Value = deletedCoupon
            };
        }

        public async Task<ApplicationResponse<bool>> ExistsByIdAsync(int id)
        {
            var exists = _couponRepository.ExistsAsync(i => i.Id == id).GetAwaiter().GetResult().Value;
            return new ApplicationResponse<bool>
            {
                IsSuccess = true,
                Value = exists
            };
        }

        public async Task<ApplicationResponse<bool>> ExistsByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return new ApplicationResponse<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid coupon code"
                };
            }

            var exists = _couponRepository.ExistsAsync(c => c.Code == code).GetAwaiter().GetResult().Value;
            return new ApplicationResponse<bool>
            {
                IsSuccess = true,
                Value = exists
            };
        }
    }
}
