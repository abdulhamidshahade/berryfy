using Berryfy.Domain.Entities.AuthEntities;

namespace Berryfy.Application.Dtos.AuthDtos.Responses
{
    public class RoleResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string concurrencyStamp { get; set; }
        public string normalizedName { get; set; }

        public static RoleResponse MapFromRole(Role role)
        {
            return new RoleResponse
            {
                Id = role.Id,
                Name = role.Name,
                concurrencyStamp = role.ConcurrencyStamp,
                normalizedName = role.NormalizedName
            };
        }

        public static List<RoleResponse> MapFromRole(IEnumerable<Role> roles)
        {
            List<RoleResponse> roleResponses = new List<RoleResponse>();

            foreach(Role role in roles)
            {
                roleResponses.Add(MapFromRole(role));
            }

            return roleResponses;
        }
    }
}
