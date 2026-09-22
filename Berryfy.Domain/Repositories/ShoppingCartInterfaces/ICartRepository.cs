using Berryfy.Domain.Constants;
using Berryfy.Domain.Entities.ShoppingCartEntities;

namespace Berryfy.Domain.Repositories.ShoppingCartInterfaces
{
    public interface ICartRepository
    {
        Task<Cart> UpdateCartAsync(Cart cart);
        Task<Cart> CreateCartAsync(int? userId, CartStatus status);
        Task<Cart> CreateCartAsync(string? sessionId, CartStatus status);
        Task<Cart> GetCartByUserIdAsync(int? userId, CartStatus? status = CartStatus.Active);
        Task<Cart> GetCartBySessionIdAsync(string sessionId, CartStatus? status = CartStatus.Active);
        Task<List<Cart>> GetCartsAsync();
        Task<bool> DeleteCartAsync(int? userId, string? sessionId);

        Task<Cart> UpdateItemQuantityAsync(int? userId, string? sessionId, int productId, int quantity);
        Task<CartItem> CreateItemAsync(int cartId, int? userId, string? sessionId, int productId, int quantity, decimal unitPrice);

        Task<bool> RemoveItemAsync(int? userId, string? sessionId, int productId);
        Task<Cart> UpdateCartStatusAsync(int? userId, CartStatus status);
        Task<Cart> GetCartByIdAsync(int cartId, CartStatus status);

        Task<bool> UpdateItemsAsync(List<CartItem> items);
        Task<bool> DeleteCartById(int Id);
        Task<bool> IsItemExistingByRealCart(int cartId, int productId, int userId);
        Task<bool> RemoveItemAsync(int cartId, int userId, int productId);
        Task<bool> IsConverted(int cartId);
    }
}
