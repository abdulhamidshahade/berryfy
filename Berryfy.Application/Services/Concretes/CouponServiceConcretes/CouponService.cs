using AutoMapper;
using Berryfy.Application.Dtos.CategoryDtos.Responses;
using Berryfy.Application.Dtos.CouponDtos.Requests;
using Berryfy.Application.Dtos.CouponDtos.Responses;
using Berryfy.Application.Services.Interfaces.CouponServiceInterfaces;
using Berryfy.Domain.Entities.CouponEntities;

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

        public async Task<CouponResponse> GetByIdAsync(int id)
        {
            if(id <= 0)
            {
                return null;
            }

            var coupon = await _couponRepository.GetByIdAsync(id);

            if (coupon == null)
            {
                return null;
            }

            return CouponResponse.MapFromCoupon(coupon);
        }

        public async Task<CouponResponse> GetByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            var coupon = await _couponRepository.GetByCodeAsync(code);
            if (coupon == null)
            {
                return null;
            }

            return CouponResponse.MapFromCoupon(coupon);
        }

        public async Task<IEnumerable<CouponResponse>> GetAllAsync()
        {
            var coupons = await _couponRepository.GetAllAsync();
            return CouponResponse.MapFromCoupon(coupons);
        }

        public async Task<CouponResponse> CreateAsync(CreateCoupon request)
        {
            if (request == null)
            {
                return null;
            }

            if (await ExistsByCodeAsync(request.Code))
            {
                return null;
            }

            var coupon = CreateCoupon.MapToCoupon(request);
            var createdCoupon = await _couponRepository.CreateAsync(coupon);

            return CouponResponse.MapFromCoupon(createdCoupon);
        }

        public async Task<CouponResponse> UpdateAsync(int id, UpdateCoupon request)
        {
            if (request == null)
            {
                return null;
            }

            var existingCoupon = await _couponRepository.GetByIdAsync(id);
            
            if (existingCoupon == null)
            {
                return null;
            }

            var isCouponExists = await GetByCodeAsync(request.Code);

            if(isCouponExists != null && isCouponExists.Id != request.Id)
            {
                return null;
            }

            var mappedCoupon = UpdateCoupon.MapToCoupon(request);

            var updatedCoupon = await _couponRepository.UpdateAsync(id, mappedCoupon);
            return CouponResponse.MapFromCoupon(updatedCoupon);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            if(id <= 0)
            {
                return false;
            }

            var coupon = await _couponRepository.GetByIdAsync(id);
            if (coupon == null)
            {
                return false;
            }

            var deletedCoupon = await _couponRepository.DeleteAsync(coupon);
            return deletedCoupon;
        }

        public async Task<bool> ExistsByIdAsync(int id)
        {
            return await _couponRepository.ExistsAsync(i => i.Id == id);
        }

        public async Task<bool> ExistsByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return false;
            }

            return await _couponRepository.ExistsAsync(c => c.Code == code);
        }
    }
}
