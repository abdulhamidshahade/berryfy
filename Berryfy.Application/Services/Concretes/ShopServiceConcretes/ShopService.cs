using AutoMapper;
using Berryfy.Application.Dtos.ShopDtos.Requests;
using Berryfy.Application.Dtos.ShopDtos.Responses;
using Berryfy.Application.Services.Interfaces.ShopServiceInterfaces;
using Berryfy.Domain.Entities.ShopEntities;
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

        public async Task<Shop> GetShopAsync(int id)
        {
            if(id <= 0)
            {
                return null;
            }

            var shop = await _shopRepository.GetShopAsync(id);

            if(shop == null)
            {
                return null;
            }

            return shop;
        }

        public async Task<ShopResponse> UpdateShopAsync(int id, UpdateShop shop)
        {

            if(shop == null || id <= 0)
            {
                return null;
            }

            var existingShop = await _shopRepository.GetShopAsync(id);

            if(existingShop == null)
            {
                return null;
            }

            var mappedShop = UpdateShop.MapToShop(shop);

            var updatedShop = await _shopRepository.UpdateShopAsync(mappedShop);

            if(updatedShop != null)
            {
                return ShopResponse.MapFromShop(updatedShop);
            }

            return null;
        }
    }
}
