using System.Text.Json.Serialization;

namespace Berryfy.Domain.Entities.AuthEntities
{
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NormalizedName { get; set; } = string.Empty;
        public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();
        [JsonIgnore] public List<UserRole> Users { get; set; } = new();

        public Role()
        {
        }

        public Role(string roleName)
        {
            Name = roleName;
            NormalizedName = roleName.ToUpperInvariant();
        }
    }
}
