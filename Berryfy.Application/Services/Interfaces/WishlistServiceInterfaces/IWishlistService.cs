using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.WishlistDtos.Requests;
using Berryfy.Application.Dtos.WishlistDtos.Responses;

namespace Berryfy.Application.Services.Interfaces.WishlistServiceInterfaces
{
    public interface IWishlistService
    {
        Task<ApplicationResponse<WishlistResponse>> GetByIdAsync(int id);
        Task<ApplicationResponse<WishlistResponse>> GetUserDefaultWishlistAsync(int userId);
        Task<ApplicationResponse<IEnumerable<WishlistResponse>>> GetUserWishlistsAsync(int userId);
        Task<ApplicationResponse<WishlistResponse>> CreateAsync(int userId, CreateWishlist createWishlistDto);
        Task<ApplicationResponse<WishlistResponse>> UpdateAsync(int id, UpdateWishlist updateWishlistDto);
        Task<ApplicationResponse<bool>> DeleteAsync(int id);
        Task<ApplicationResponse<bool>> ExistsAsync(int id);

        Task<ApplicationResponse<WishlistItem>> AddItemAsync(int userId, AddToWishlist addToWishlistDto);
        Task<ApplicationResponse<WishlistItem>> UpdateItemAsync(int wishlistId, int productId, UpdateWishlistItem updateItemDto);
        Task<ApplicationResponse<bool>> RemoveItemAsync(int wishlistId, int productId);
        Task<ApplicationResponse<bool>> IsProductInWishlistAsync(int userId, int productId);
        Task<ApplicationResponse<IEnumerable<WishlistItem>>> GetWishlistItemsAsync(int wishlistId);


        Task<ApplicationResponse<bool>> AddMultipleItemsAsync(int userId, int wishlistId, List<int> productIds);
        Task<ApplicationResponse<bool>> RemoveMultipleItemsAsync(int wishlistId, List<int> productIds);
        Task<ApplicationResponse<bool>> MoveItemsToWishlistAsync(int fromWishlistId, int toWishlistId, List<int> productIds);
        Task<ApplicationResponse<bool>> ClearWishlistAsync(int wishlistId);

        Task<ApplicationResponse<WishlistSummary>> GetUserSummaryAsync(int userId);
        Task<ApplicationResponse<bool>> ShareWishlistAsync(int wishlistId, bool isPublic);
        Task<ApplicationResponse<WishlistResponse>> DuplicateWishlistAsync(int wishlistId, string newName);

        Task<ApplicationResponse<IEnumerable<WishlistResponse>>> GetAllWishlistsAsync();
        Task<ApplicationResponse<GlobalWishlistStats>> GetGlobalStatsAsync();
    }
}
