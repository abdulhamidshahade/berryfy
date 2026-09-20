namespace Berryfy.Application.Dtos.WishlistDtos.Requests
{
    public class AddToWishlist
    {
        public int ProductId { get; set; }
        public int? WishlistId { get; set; }
        public string? Notes { get; set; }
        public int Priority { get; set; } = 1;
    }
}
