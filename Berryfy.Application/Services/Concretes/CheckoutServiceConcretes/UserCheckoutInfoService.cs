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

        public async Task<UserCheckoutInfoResponse?> GetCheckoutInfoAsync(int userId)
        {
            var checkoutInfo = await _repository.GetByUserIdAsync(userId);

            if (checkoutInfo == null) return null;

            // Update last used timestamp
            await _repository.UpdateLastUsedAsync(checkoutInfo.Id);

            return MapToDto(checkoutInfo);
        }

        public async Task<UserCheckoutInfoResponse> SaveCheckoutInfoAsync(int userId, SaveCheckoutInfo dto)
        {
            var existing = await _repository.GetByUserIdAsync(userId);

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
                return MapToDto(existing);
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

                var created = await _repository.CreateAsync(newCheckoutInfo);
                return MapToDto(created);
            }
        }

        public async Task<UserCheckoutInfoResponse> SavePaymentBillingInfoAsync(int userId, SavePaymentBilling dto)
        {
            var existing = await _repository.GetByUserIdAsync(userId);

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
                return MapToDto(existing);
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

                var created = await _repository.CreateAsync(newCheckoutInfo);
                return MapToDto(created);
            }
        }

        public async Task<bool> DeleteCheckoutInfoAsync(int userId)
        {
            var existing = await _repository.GetByUserIdAsync(userId);

            if (existing == null) return false;

            return await _repository.DeleteAsync(existing.Id);
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
