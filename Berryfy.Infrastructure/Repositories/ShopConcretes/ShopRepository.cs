using Berryfy.Domain.Entities.ShopEntities;
using Berryfy.Domain.Repositories;
using Berryfy.Domain.Repositories.ShopInterfaces;
using Berryfy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Berryfy.Infrastructure.Repositories.ShopConcretes
{
    public class ShopRepository : IShopRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork _unitOfWork;

        public ShopRepository(ApplicationDbContext context,
                              IUnitOfWork unitOfWork)
        {
            _context = context;
            _unitOfWork = unitOfWork;
        }

        public async Task<Shop> GetShopAsync(int id)
        {
            return await _context.Shops.
                Where(i => i.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<Shop> UpdateShopAsync(Shop shop)
        {
            _context.Shops.Update(shop);
            
            if(await _unitOfWork.SaveDbChangesAsync())
            {
                return shop;
            }

            return null;
        }
    }
}
