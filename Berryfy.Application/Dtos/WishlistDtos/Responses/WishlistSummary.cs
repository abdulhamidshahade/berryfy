namespace Berryfy.Application.Dtos.WishlistDtos.Responses
{
    public class WishlistSummary
    {
        public int TotalWishlists { get; set; }
        public int TotalItems { get; set; }
        public decimal TotalValue { get; set; }
        public List<WishlistResponse> RecentWishlists { get; set; }
    }
}
