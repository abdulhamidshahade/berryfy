using System.Text.Json.Serialization;
using Berryfy.Domain.Entities.AuthEntities;
using Berryfy.Domain.Entities.Base;
using System.ComponentModel.DataAnnotations;

namespace Berryfy.Domain.Entities.WishlistEntities
{
    public class Wishlist : IAuditableEntity
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }

        public string? Name { get; set; } = "My Wishlist";

        public bool IsDefault { get; set; } = true;

        public bool IsPublic { get; set; } = false;

        [JsonIgnore] public User User { get; set; }
        [JsonIgnore] public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();

        public int ItemCount => WishlistItems?.Count ?? 0;
        public decimal TotalValue => WishlistItems?.Sum(x => x.Product?.Price ?? 0) ?? 0;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
