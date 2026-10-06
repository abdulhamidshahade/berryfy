using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.CheckoutDtos.Requests;
using Berryfy.Application.Dtos.CheckoutDtos.Responses;
using Berryfy.Application.Services.Interfaces.CheckoutServiceInterfaces;
using Berryfy.Domain.Entities.CheckoutEntities;
using Berryfy.Domain.Repositories.CheckoutInterfaces;

namespace Berryfy.Application.Services.Concretes.CheckoutServiceConcretes
{
    public class UserCheckoutInfoService : IUserCheckoutInfoService
    {
        private readonly IUserCheckoutInfoRepository _repository;

        public UserCheckoutInfoService(IUserCheckoutInfoRepository repository)
        {
            _repository = repository;
        }

        public async Task<ApplicationResponse<UserCheckoutInfoResponse?>> GetCheckoutInfoAsync(int userId)
        {
            var checkoutInfo = _repository.GetByUserIdAsync(userId).GetAwaiter().GetResult().Value;

            if (checkoutInfo == null) return new ApplicationResponse<UserCheckoutInfoResponse?>();


            // Update last used timestamp
            await _repository.UpdateLastUsedAsync(checkoutInfo.Id);

            return new ApplicationResponse<UserCheckoutInfoResponse?>
            {
                IsSuccess = true,
                SuccessMessage = "Checkout info retrieved successfully",
                Value = MapToDto(checkoutInfo)
            };
        }

        public async Task<ApplicationResponse<UserCheckoutInfoResponse?>> SaveCheckoutInfoAsync(int userId, SaveCheckoutInfo dto)
        {
            var existing = _repository.GetByUserIdAsync(userId).GetAwaiter().GetResult().Value;

            if (existing != null)
            {
                // Update existing
                existing.FirstName = dto.FirstName;
                existing.LastName = dto.LastName;
                existing.Email = dto.Email;
                existing.Phone = dto.Phone;
                existing.Address = dto.Address;
                existing.Address2 = dto.Address2;
                existing.City = dto.City;
                existing.State = dto.State;
                existing.ZipCode = dto.ZipCode;
                existing.Country = dto.Country;

                await _repository.UpdateAsync(existing);
                return new ApplicationResponse<UserCheckoutInfoResponse?>
                {
                    IsSuccess = true,
                    SuccessMessage = "Checkout info updated successfully",
                    Value = MapToDto(existing)
                };
            }
            else
            {
                // Create new
                var newCheckoutInfo = new UserCheckoutInfo
                {
                    UserId = userId,
                    SessionId = null,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Email = dto.Email,
                    Phone = dto.Phone,
                    Address = dto.Address,
                    Address2 = dto.Address2,
                    City = dto.City,
                    State = dto.State,
                    ZipCode = dto.ZipCode,
                    Country = dto.Country
                };

                var created = _repository.CreateAsync(newCheckoutInfo).GetAwaiter().GetResult().Value;
                return new ApplicationResponse<UserCheckoutInfoResponse?>
                {
                    IsSuccess = true,
                    SuccessMessage = "Checkout info created successfully",
                    Value = MapToDto(created)
                };
            }
        }

        public async Task<ApplicationResponse<UserCheckoutInfoResponse?>> SavePaymentBillingInfoAsync(int userId, SavePaymentBilling dto)
        {
            var existing = _repository.GetByUserIdAsync(userId).GetAwaiter().GetResult().Value;

            if (existing != null)
            {
                // Update billing info
                existing.PayerName = dto.PayerName;
                existing.PayerEmail = dto.PayerEmail;
                existing.BillingAddress1 = dto.BillingAddress1;
                existing.BillingAddress2 = dto.BillingAddress2;
                existing.BillingCity = dto.BillingCity;
                existing.BillingState = dto.BillingState;
                existing.BillingPostalCode = dto.BillingPostalCode;
                existing.BillingCountry = dto.BillingCountry;

                await _repository.UpdateAsync(existing);
                return new ApplicationResponse<UserCheckoutInfoResponse?>
                {
                    IsSuccess = true,
                    SuccessMessage = "Payment billing info updated successfully",
                    Value = MapToDto(existing)
                };
            }
            else
            {
                // Create new with billing info only
                var newCheckoutInfo = new UserCheckoutInfo
                {
                    UserId = userId,
                    SessionId = null,
                    FirstName = string.Empty,
                    LastName = string.Empty,
                    Email = dto.PayerEmail,
                    Address = dto.BillingAddress1,
                    City = dto.BillingCity,
                    State = dto.BillingState,
                    ZipCode = dto.BillingPostalCode,
                    Country = dto.BillingCountry,
                    PayerName = dto.PayerName,
                    PayerEmail = dto.PayerEmail,
                    BillingAddress1 = dto.BillingAddress1,
                    BillingAddress2 = dto.BillingAddress2,
                    BillingCity = dto.BillingCity,
                    BillingState = dto.BillingState,
                    BillingPostalCode = dto.BillingPostalCode,
                    BillingCountry = dto.BillingCountry
                };

                var created = _repository.CreateAsync(newCheckoutInfo).GetAwaiter().GetResult().Value;
                return new ApplicationResponse<UserCheckoutInfoResponse?>
                {
                    IsSuccess = true,
                    SuccessMessage = "Checkout info created successfully",
                    Value = MapToDto(created)
                };
            }
        }

        public async Task<ApplicationResponse<bool>> DeleteCheckoutInfoAsync(int userId)
        {
            var existing = _repository.GetByUserIdAsync(userId).GetAwaiter().GetResult().Value;

            if (existing == null) return new ApplicationResponse<bool>
            {
                IsSuccess = false,
                ErrorMessage = "Checkout info not found"
            };

            return new ApplicationResponse<bool>
            {
                IsSuccess = true,
                SuccessMessage = "Checkout info deleted successfully",
                Value = _repository.DeleteAsync(existing.Id).GetAwaiter().GetResult().Value
            };
        }

        private UserCheckoutInfoResponse MapToDto(UserCheckoutInfo entity)
        {
            return new UserCheckoutInfoResponse
            {
                Id = entity.Id,
                UserId = entity.UserId,
                SessionId = entity.SessionId,
                FirstName = entity.FirstName,
                LastName = entity.LastName,
                Email = entity.Email,
                Phone = entity.Phone,
                Address = entity.Address,
                Address2 = entity.Address2,
                City = entity.City,
                State = entity.State,
                ZipCode = entity.ZipCode,
                Country = entity.Country,
                PayerName = entity.PayerName,
                PayerEmail = entity.PayerEmail,
                BillingAddress1 = entity.BillingAddress1,
                BillingAddress2 = entity.BillingAddress2,
                BillingCity = entity.BillingCity,
                BillingState = entity.BillingState,
                BillingPostalCode = entity.BillingPostalCode,
                BillingCountry = entity.BillingCountry,
                LastUsedAt = entity.LastUsedAt
            };
        }
    }
}