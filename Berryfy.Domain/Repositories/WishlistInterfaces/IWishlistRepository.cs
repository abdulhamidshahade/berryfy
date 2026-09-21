using Berryfy.Domain.Entities.WishlistEntities;
using Berryfy.Application.Dtos.WishlistDtos;

namespace Berryfy.Domain.Repositories.WishlistInterfaces
{
    public interface IWishlistRepository
    {
        Task<Wishlist> GetByIdAsync(int id);
        Task<Wishlist> GetUserDefaultWishlistAsync(int userId);
        Task<IEnumerable<Wishlist>> GetUserWishlistsAsync(int userId);
        Task<Wishlist> CreateAsync(Wishlist wishlist);
        Task<Wishlist> UpdateAsync(Wishlist wishlist);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);

        Task<WishlistItem> GetWishlistItemAsync(int wishlistId, int productId);
        Task<WishlistItem> AddItemAsync(WishlistItem item);
        Task<WishlistItem> UpdateItemAsync(WishlistItem item);
        Task<bool> RemoveItemAsync(int wishlistId, int productId);
        Task<bool> IsProductInWishlistAsync(int userId, int productId);
        Task<IEnumerable<WishlistItem>> GetWishlistItemsAsync(int wishlistId);

        Task<int> GetUserWishlistCountAsync(int userId);
        Task<int> GetUserTotalItemsAsync(int userId);
        Task<decimal> GetUserTotalValueAsync(int userId);

        Task<IEnumerable<Wishlist>> GetAllWishlistsAsync();
        //Task<GlobalWishlistStats> GetGlobalStatsAsync();
    }
}
