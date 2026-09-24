using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.ShopDtos.Requests;
using Berryfy.Application.Dtos.ShopDtos.Responses;
using Berryfy.Domain.Entities.ShopEntities;

namespace Berryfy.Application.Services.Interfaces.ShopServiceInterfaces
{
    public interface IShopService
    {
        Task<ApplicationResponse<ShopResponse>> GetShopAsync(int id);
        Task<ApplicationResponse<ShopResponse>> UpdateShopAsync(int id, UpdateShop shop);
    }
}