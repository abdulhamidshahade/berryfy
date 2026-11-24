using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.ShopDtos.Requests;
using Berryfy.Application.Dtos.ShopDtos.Responses;
using Berryfy.Application.Services.Interfaces.ShopServiceInterfaces;
using Berryfy.Domain.Repositories.ShopInterfaces;

namespace Berryfy.Application.Services.Concretes.ShopServiceConcretes
{
    public class ShopService : IShopService
    {
        private readonly IShopRepository _shopRepository;

        public ShopService(IShopRepository shopRepository)
        {
            _shopRepository = shopRepository;
        }

        public async Task<ApplicationResponse<ShopResponse>> GetShopAsync(int id)
        {
            if(id <= 0)
            {
                return new ApplicationResponse<ShopResponse>()
                {
                    IsSuccess = false,
                    Value = null,
                    ErrorMessage = "Shop id should be non-zero positive value"
                };
            }

            var shop = await _shopRepository.GetShopAsync(id);

            if (shop == null || !shop.IsSuccess || shop.Value == null)
            {
                return new ApplicationResponse<ShopResponse>()
                {
                    IsSuccess = false,
                    Value = null,
                    ErrorMessage = "Shop not found"
                };
            }

            return new ApplicationResponse<ShopResponse>()
            {
                IsSuccess = true,
                SuccessMessage = "Shop data retrieved successfully",
                Value = ShopResponse.MapFromShop(shop.Value)
            };
        }

        public async Task<ApplicationResponse<ShopResponse>> UpdateShopAsync(int id, UpdateShop shop)
        {
            if (shop == null || id <= 0)
            {
                return new ApplicationResponse<ShopResponse>()
                {
                    IsSuccess = false,
                    Value = null,
                    ErrorMessage = "id should be non-zero positive and shop should not be null"
                };
            }

            var existingShop = await _shopRepository.GetShopAsync(id);

            if (existingShop == null || !existingShop.IsSuccess || existingShop.Value == null)
            {
                return new ApplicationResponse<ShopResponse>()
                {
                    IsSuccess = false,
                    Value = null,
                    ErrorMessage = "Shop not found"
                };
            }

            var mappedShop = UpdateShop.MapToShop(shop);
            mappedShop.Id = id;
            var updatedShop = await _shopRepository.UpdateShopAsync(mappedShop);

            if (updatedShop != null && updatedShop.IsSuccess && updatedShop.Value != null)
            {
                return new ApplicationResponse<ShopResponse>()
                {
                    IsSuccess = true,
                    SuccessMessage = "Shop updated successfully",
                    Value = ShopResponse.MapFromShop(updatedShop.Value)
                };
            }

            return new ApplicationResponse<ShopResponse>()
            {
                IsSuccess = false,
                SuccessMessage = "Something went wrong while updating the shop",
                Value = null
            };
        }
    }
}