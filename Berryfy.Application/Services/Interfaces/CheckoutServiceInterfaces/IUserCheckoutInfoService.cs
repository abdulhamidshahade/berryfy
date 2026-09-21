using Berryfy.Application.Dtos.CheckoutDtos.Requests;
using Berryfy.Application.Dtos.CheckoutDtos.Responses;

namespace Berryfy.Application.Services.Interfaces.CheckoutServiceInterfaces
{
    public interface IUserCheckoutInfoService
    {
        Task<UserCheckoutInfoResponse?> GetCheckoutInfoAsync(int userId);
        Task<UserCheckoutInfoResponse> SaveCheckoutInfoAsync(int userId, SaveCheckoutInfo dto);
        Task<UserCheckoutInfoResponse> SavePaymentBillingInfoAsync(int userId, SavePaymentBilling dto);
        Task<bool> DeleteCheckoutInfoAsync(int userId);
    }
}
