using Berryfy.Application.Dtos.ShoppingCartDtos.Responses;
using Berryfy.Domain.Constants;

namespace Berryfy.Application.Services.Interfaces.ShoppingCartServiceInterfaces
{
    public interface ICartService
    {
        Task<CartResponse> GetCartByUserIdAsync(int userId, CartStatus? status = CartStatus.Active);
        Task<CartResponse> GetCartBySessionIdAsync(string sessionId, CartStatus? status = CartStatus.Active);
        Task<CartResponse> GetCartByIdAsync(int cartId, CartStatus status);
        Task<CartItemResponse> GetItemAsync(int cartId, int productId);
        Task<CartResponse> CreateCartAsync(int? userId, string? sessionId);
        Task<CartResponse?> AddItemAsync(int cartId, int? userId, string? sessionId, int productId, int quantity);
        Task<CartResponse> UpdateItemQuantityAsync(int cartId, int? userId, string? sessionId, int productId, int quantity);
        Task<bool> RemoveItemAsync(int cartId, int? userId, string? sessionId, int productId);
        Task<bool> ClearCartAsync(int cartId, int? userId, string? sessionId);
        Task<bool> CompleteCartAsync(int cartId, int? userId);
        Task<bool> ConvertCartAsync(int cartId);
        Task<bool> UpdateCartStatusAsync(int cartId, CartStatus status);
        Task<bool> ReactivateCartAsync(int cartId, int orderId);
        Task<bool> HandleAbandonedCartAsync(int cartId);
        Task<int> CleanupExpiredCartsAsync();
        Task<CartResponse> RefreshCartAsync(int cartId);
        Task<CartResponse> ApplyCouponAsync(int cartId, int? userId, string couponCode);
        Task<CartResponse> RemoveCouponAsync(int cartId, int? userId, string? sessionId, int couponId);
        Task MergeCartAsync(int userId, string sessionId);
        
    }
}
