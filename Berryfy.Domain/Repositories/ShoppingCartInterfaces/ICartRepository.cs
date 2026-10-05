using Berryfy.Domain.Constants;
using Berryfy.Domain.Entities;
using Berryfy.Domain.Entities.ShoppingCartEntities;

namespace Berryfy.Domain.Repositories.ShoppingCartInterfaces
{
    public interface ICartRepository
    {
        Task<InfrastructureResponse<Cart>> UpdateCartAsync(Cart cart);
        Task<InfrastructureResponse<Cart>> CreateCartAsync(int? userId, CartStatus status);
        Task<InfrastructureResponse<Cart>> CreateCartAsync(string? sessionId, CartStatus status);
        Task<InfrastructureResponse<Cart>> GetCartByUserIdAsync(int? userId, CartStatus? status = CartStatus.Active);
        Task<InfrastructureResponse<Cart>> GetCartBySessionIdAsync(string sessionId, CartStatus? status = CartStatus.Active);
        Task<InfrastructureResponse<List<Cart>>> GetCartsAsync();
        Task<InfrastructureResponse<bool>> DeleteCartAsync(int? userId, string? sessionId);

        Task<InfrastructureResponse<Cart>> UpdateItemQuantityAsync(int? userId, string? sessionId, int productId, int quantity);
        Task<InfrastructureResponse<CartItem>> CreateItemAsync(int cartId, int? userId, string? sessionId, int productId, int quantity, decimal unitPrice);

        Task<InfrastructureResponse<bool>> RemoveItemAsync(int? userId, string? sessionId, int productId);
        Task<InfrastructureResponse<Cart>> UpdateCartStatusAsync(int? userId, CartStatus status);
        Task<InfrastructureResponse<Cart>> GetCartByIdAsync(int cartId, CartStatus status);

        Task<InfrastructureResponse<bool>> UpdateItemsAsync(List<CartItem> items);
        Task<InfrastructureResponse<bool>> DeleteCartById(int Id);
        Task<InfrastructureResponse<bool>> IsItemExistingByRealCart(int cartId, int productId, int userId);
        Task<InfrastructureResponse<bool>> RemoveItemAsync(int cartId, int userId, int productId);
        Task<InfrastructureResponse<bool>> IsConverted(int cartId);
    }
}
