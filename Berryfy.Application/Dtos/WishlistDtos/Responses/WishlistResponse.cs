namespace Berryfy.Application.Dtos.WishlistDtos.Responses
{
    public class WishlistResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }
        public bool IsDefault { get; set; }
        public bool IsPublic { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public int ItemCount { get; set; }
        public decimal TotalValue { get; set; }
        public List<WishlistItem> Items { get; set; }
    }
}
