using Microsoft.AspNetCore.Identity;

namespace Berryfy.Domain.Entities.AuthEntities
{ 
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string NormalizedName { get; set; }
        public string ConcurrencyStamp { get; set; }
        public List<UserRole> Users { get; set; }

        public Role()
        {

        }
        public Role(string roleName)
        {
            this.Name = roleName;
        }
    }
}
