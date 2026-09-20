namespace Berryfy.Application.Dtos.WishlistDtos.Requests
{
    public class CreateWishlist
    {
        public string Name { get; set; } = "My Wishlist";
        public bool IsPublic { get; set; } = false;
    }
}
