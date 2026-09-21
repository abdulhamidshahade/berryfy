using Berryfy.Application.Dtos.WishlistDtos.Requests;
using Berryfy.Application.Dtos.WishlistDtos.Responses;

namespace Berryfy.Application.Services.Interfaces.WishlistServiceInterfaces
{
    public interface IWishlistService
    {
        Task<WishlistResponse> GetByIdAsync(int id);
        Task<WishlistResponse> GetUserDefaultWishlistAsync(int userId);
        Task<IEnumerable<WishlistResponse>> GetUserWishlistsAsync(int userId);
        Task<WishlistResponse> CreateAsync(int userId, CreateWishlist createWishlistDto);
        Task<WishlistResponse> UpdateAsync(int id, UpdateWishlist updateWishlistDto);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);

        Task<WishlistItem> AddItemAsync(int userId, AddToWishlist addToWishlistDto);
        Task<WishlistItem> UpdateItemAsync(int wishlistId, int productId, UpdateWishlistItem updateItemDto);
        Task<bool> RemoveItemAsync(int wishlistId, int productId);
        Task<bool> IsProductInWishlistAsync(int userId, int productId);
        Task<IEnumerable<WishlistItem>> GetWishlistItemsAsync(int wishlistId);


        Task<bool> AddMultipleItemsAsync(int userId, int wishlistId, List<int> productIds);
        Task<bool> RemoveMultipleItemsAsync(int wishlistId, List<int> productIds);
        Task<bool> MoveItemsToWishlistAsync(int fromWishlistId, int toWishlistId, List<int> productIds);
        Task<bool> ClearWishlistAsync(int wishlistId);

        Task<WishlistSummary> GetUserSummaryAsync(int userId);
        Task<bool> ShareWishlistAsync(int wishlistId, bool isPublic);
        Task<WishlistResponse> DuplicateWishlistAsync(int wishlistId, string newName);

        Task<IEnumerable<WishlistResponse>> GetAllWishlistsAsync();
        Task<GlobalWishlistStats> GetGlobalStatsAsync();
    }
}
