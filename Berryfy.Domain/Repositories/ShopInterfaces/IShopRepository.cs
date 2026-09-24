using Berryfy.Domain.Entities;
using Berryfy.Domain.Entities.ShopEntities;

namespace Berryfy.Domain.Repositories.ShopInterfaces
{
    public interface IShopRepository
    {
        Task<InfrastructureResponse<Shop>> GetShopAsync(int id);
        Task<InfrastructureResponse<Shop>> UpdateShopAsync(Shop shop);
    }
}
