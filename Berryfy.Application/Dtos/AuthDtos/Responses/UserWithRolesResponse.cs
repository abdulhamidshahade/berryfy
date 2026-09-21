using Berryfy.Application.Services.Concretes.AuthServiceConcretes;
using Berryfy.Domain.Entities.AuthEntities;

namespace Berryfy.Application.Dtos.AuthDtos.Responses
{
    public class UserWithRolesResponse
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public List<string> Roles { get; set; }
        public bool EmailConfirmed { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public int AccessFailedCount { get; set; }]


        public static UserWithRolesResponse MapFromUser(User user)
        {
            return new UserWithRolesResponse
            {
                Id = user.Id,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                LockoutEnd = user.LockoutEnd,
                AccessFailedCount = user.AccessFailedCount,
                Roles = user.Roles.Select(r => new RoleManagementService(). r.RoleId).ToList()
            };
        }
    }
}
