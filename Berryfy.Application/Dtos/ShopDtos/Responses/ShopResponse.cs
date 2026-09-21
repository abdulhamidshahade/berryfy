using Berryfy.Domain.Entities.ShopEntities;

namespace Berryfy.Application.Dtos.ShopDtos.Responses
{
    public class ShopResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string LogoUrl { get; set; }
        public string Description { get; set; }

        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }

        public string Currency { get; set; }
        public string Language { get; set; }

        public static ShopResponse MapFromShop(Shop shop)
        {
            return new ShopResponse
            {
                Id = shop.Id,
                Name = shop.Name,
                LogoUrl = shop.LogoUrl,
                Description = shop.Description,
                Email = shop.Email,
                Phone = shop.Phone,
                Address = shop.Address,
                Currency = shop.Currency,
                Language = shop.Language
            };
        }
    }
}
