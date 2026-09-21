using Berryfy.Application.Dtos.ShopDtos.Requests;
using Berryfy.Application.Dtos.ShopDtos.Responses;
using Berryfy.Domain.Entities.ShopEntities;

namespace Berryfy.Application.Services.Interfaces.ShopServiceInterfaces
{
    public interface IShopService
    {
        Task<Shop> GetShopAsync(int id);
        Task<ShopResponse> UpdateShopAsync(int id, UpdateShop shop);
    }
}
