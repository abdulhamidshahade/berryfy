using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.ShoppingCartDtos.Responses;
using Berryfy.Domain.Constants;

namespace Berryfy.Application.Services.Interfaces.ShoppingCartServiceInterfaces
{
    public interface ICartService
    {
        Task<ApplicationResponse<CartResponse>> GetCartByUserIdAsync(int userId, CartStatus? status = CartStatus.Active);
        Task<ApplicationResponse<CartResponse>> GetCartBySessionIdAsync(string sessionId, CartStatus? status = CartStatus.Active);
        Task<ApplicationResponse<CartResponse>> GetCartByIdAsync(int cartId, CartStatus status);
        Task<ApplicationResponse<CartItemResponse>> GetItemAsync(int cartId, int productId);
        Task<ApplicationResponse<CartResponse>> CreateCartAsync(int? userId, string? sessionId);
        Task<ApplicationResponse<CartResponse?>> AddItemAsync(int cartId, int? userId, string? sessionId, int productId, int quantity);
        Task<ApplicationResponse<CartResponse>> UpdateItemQuantityAsync(int cartId, int? userId, string? sessionId, int productId, int quantity);
        Task<ApplicationResponse<bool>> RemoveItemAsync(int cartId, int? userId, string? sessionId, int productId);
        Task<ApplicationResponse<bool>> ClearCartAsync(int cartId, int? userId, string? sessionId);
        Task<ApplicationResponse<bool>> CompleteCartAsync(int cartId, int? userId);
        Task<ApplicationResponse<bool>> ConvertCartAsync(int cartId);
        Task<ApplicationResponse<bool>> UpdateCartStatusAsync(int cartId, CartStatus status);
        Task<ApplicationResponse<bool>> ReactivateCartAsync(int cartId, int orderId);
        Task<ApplicationResponse<bool>> HandleAbandonedCartAsync(int cartId);
        Task<ApplicationResponse<int>> CleanupExpiredCartsAsync();
        Task<ApplicationResponse<CartResponse>> RefreshCartAsync(int cartId);
        Task<ApplicationResponse<CartResponse>> ApplyCouponAsync(int cartId, int? userId, string couponCode);
        Task<ApplicationResponse<CartResponse>> RemoveCouponAsync(int cartId, int? userId, string? sessionId, int couponId);
        Task<ApplicationResponse<bool>> MergeCartAsync(int userId, string sessionId);
        
    }
}