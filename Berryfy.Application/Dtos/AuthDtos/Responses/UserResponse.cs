using Berryfy.Domain.Entities.AuthEntities;

namespace Berryfy.Application.Dtos.AuthDtos.Responses
{
    public class UserResponse
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();

        public static UserResponse MapFromUser(User user, IEnumerable<string>? roles = null)
        {
            return new UserResponse
            {
                Id = user.Id,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Roles = roles?.ToList() ?? new List<string>()
            };
        }

        public static List<UserResponse> MapFromUser(IEnumerable<User> users)
        {
            return users.Select(user => MapFromUser(user)).ToList();
        }
    }
}
