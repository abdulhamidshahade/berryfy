using Berryfy.Domain.Entities;
using Berryfy.Domain.Entities.CheckoutEntities;

namespace Berryfy.Domain.Repositories.CheckoutInterfaces
{
    public interface IUserCheckoutInfoRepository
    {
        Task<InfrastructureResponse<UserCheckoutInfo?>> GetByUserIdAsync(int userId);
        Task<InfrastructureResponse<UserCheckoutInfo?>> GetBySessionIdAsync(string sessionId);
        Task<InfrastructureResponse<UserCheckoutInfo>> CreateAsync(UserCheckoutInfo checkoutInfo);
        Task<InfrastructureResponse<bool>> UpdateAsync(UserCheckoutInfo checkoutInfo);
        Task<InfrastructureResponse<bool>> DeleteAsync(int id);
        Task<InfrastructureResponse<bool>> UpdateLastUsedAsync(int id);
    }
}
