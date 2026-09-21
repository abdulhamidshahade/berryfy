using Berryfy.Domain.Entities.ShopEntities;

namespace Berryfy.Application.Dtos.ShopDtos.Requests
{
    public class UpdateShop
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

        public static Shop MapToShop(UpdateShop request)
        {
            return new Shop
            {
                Id = request.Id,
                Name = request.Name,
                LogoUrl = request.LogoUrl,
                Description = request.Description,
                Email = request.Email,
                Phone = request.Phone,
                Address = request.Address,
                Currency = request.Currency,
                Language = request.Language
            };
        }
    }
}
