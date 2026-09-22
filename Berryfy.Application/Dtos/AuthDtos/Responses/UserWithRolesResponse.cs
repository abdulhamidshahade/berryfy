using Berryfy.Domain.Entities.AuthEntities;

namespace Berryfy.Application.Dtos.AuthDtos.Responses
{
    public class UserWithRolesResponse
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public bool EmailConfirmed { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public int AccessFailedCount { get; set; }

        public static UserWithRolesResponse MapFromUser(User user, IEnumerable<string>? roles = null)
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
                Roles = roles?.ToList() ?? new List<string>()
            };
        }
    }
}
