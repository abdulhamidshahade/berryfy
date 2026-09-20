namespace Berryfy.Application.Dtos.AuthDtos.Requests
{
    public class CreateUser
    {
        public string Email { get; set; }
        public string UserName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string Password { get; set; }
        public bool EmailConfirmed { get; set; } = false;
        public List<string> Roles { get; set; } = new List<string> { "User" };
    }
}