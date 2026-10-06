using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.CheckoutDtos.Requests;
using Berryfy.Application.Dtos.CheckoutDtos.Responses;

namespace Berryfy.Application.Services.Interfaces.CheckoutServiceInterfaces
{
    public interface IUserCheckoutInfoService
    {
        Task<ApplicationResponse<UserCheckoutInfoResponse?>> GetCheckoutInfoAsync(int userId);
        Task<ApplicationResponse<UserCheckoutInfoResponse>> SaveCheckoutInfoAsync(int userId, SaveCheckoutInfo dto);
        Task<ApplicationResponse<UserCheckoutInfoResponse>> SavePaymentBillingInfoAsync(int userId, SavePaymentBilling dto);
        Task<ApplicationResponse<bool>> DeleteCheckoutInfoAsync(int userId);
    }
}
