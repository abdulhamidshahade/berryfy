namespace Berryfy.Application.Dtos.WishlistDtos.Responses
{
    public class GlobalWishlistStats
    {
        public int TotalUsers { get; set; }
        public int TotalWishlists { get; set; }
        public int TotalItems { get; set; }
        public decimal TotalValue { get; set; }
        public double AverageItemsPerWishlist { get; set; }
        public double AverageWishlistsPerUser { get; set; }
        public int PublicWishlists { get; set; }
        public int PrivateWishlists { get; set; }
        public List<RecentActivity> RecentActivity { get; set; }
    }
}
