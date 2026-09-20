using Berryfy.Application.Dtos.ProductDtos.Responses;

namespace Berryfy.Application.Dtos.WishlistDtos.Responses
{
    public class WishlistItem
    {
        public int Id { get; set; }
        public int WishlistId { get; set; }
        public int ProductId { get; set; }
        public string? Notes { get; set; }
        public int Priority { get; set; }
        public DateTime AddedDate { get; set; }
        public ProductResponse Product { get; set; }
    }
}
