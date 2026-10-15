using Berryfy.Application.Dtos.ProductDtos.Responses;
using Berryfy.Domain.Entities.ShoppingCartEntities;

namespace Berryfy.Application.Dtos.ShoppingCartDtos.Responses
{
    public class CartItemResponse
    {
        public int Id { get; set; }
        public int? ShoppingCartId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public ProductResponse Product { get; set; }

        public static CartItem MapToCartItem(CartItem cartItem)
        {
            return new CartItem()
            {
                Id = cartItem.Id,
                ShoppingCartId = cartItem.ShoppingCartId,
                ProductId = cartItem.ProductId,
                Quantity = cartItem.Quantity,
                UnitPrice = cartItem.UnitPrice,
            };
        }

        public static CartItemResponse MapFromCartItem(CartItem cartItem)
        {
            return new CartItemResponse()
            {
                Id = cartItem.Id,
                ShoppingCartId = cartItem.ShoppingCartId,
                ProductId = cartItem.ProductId,
                Quantity = cartItem.Quantity,
                UnitPrice = cartItem.UnitPrice,
            };
        }
    }
}
