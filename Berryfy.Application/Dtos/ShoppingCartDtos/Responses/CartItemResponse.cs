using Berryfy.Application.Dtos.ProductDtos.Responses;

namespace Berryfy.Application.Dtos.ShoppingCartDtos.Responses
{
    public class CartItemResponse
    {
        public int Id { get; set; }
        public int ShoppingCartId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public ProductResponse Product { get; set; }
    }
}
