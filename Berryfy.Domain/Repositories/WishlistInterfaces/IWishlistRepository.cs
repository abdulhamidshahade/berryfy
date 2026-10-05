using Berryfy.Application.Dtos.WishlistDtos;
using Berryfy.Domain.Entities;
using Berryfy.Domain.Entities.WishlistEntities;

namespace Berryfy.Domain.Repositories.WishlistInterfaces
{
    public interface IWishlistRepository
    {
        Task<InfrastructureResponse<Wishlist>> GetByIdAsync(int id);
        Task<InfrastructureResponse<Wishlist>> GetUserDefaultWishlistAsync(int userId);
        Task<InfrastructureResponse<IEnumerable<Wishlist>>> GetUserWishlistsAsync(int userId);
        Task<InfrastructureResponse<Wishlist>> CreateAsync(Wishlist wishlist);
        Task<InfrastructureResponse<Wishlist>> UpdateAsync(Wishlist wishlist);
        Task<InfrastructureResponse<bool>> DeleteAsync(int id);
        Task<InfrastructureResponse<bool>> ExistsAsync(int id);

        Task<InfrastructureResponse<WishlistItem>> GetWishlistItemAsync(int wishlistId, int productId);
        Task<InfrastructureResponse<WishlistItem>> AddItemAsync(WishlistItem item);
        Task<InfrastructureResponse<WishlistItem>> UpdateItemAsync(WishlistItem item);
        Task<InfrastructureResponse<bool>> RemoveItemAsync(int wishlistId, int productId);
        Task<InfrastructureResponse<bool>> IsProductInWishlistAsync(int userId, int productId);
        Task<InfrastructureResponse<IEnumerable<WishlistItem>>> GetWishlistItemsAsync(int wishlistId);

        Task<InfrastructureResponse<int>> GetUserWishlistCountAsync(int userId);
        Task<InfrastructureResponse<int>> GetUserTotalItemsAsync(int userId);
        Task<InfrastructureResponse<decimal>> GetUserTotalValueAsync(int userId);

        Task<InfrastructureResponse<IEnumerable<Wishlist>>> GetAllWishlistsAsync();
        Task<InfrastructureResponse<GlobalWishlistStats>> GetGlobalStatsAsync();
    }
}
