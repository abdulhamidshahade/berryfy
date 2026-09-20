namespace Berryfy.Application.Dtos.AuthDtos.Requests
{
    public class UpdateUserRequest
    {
        public string Email { get; set; }
        public string UserName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public bool EmailConfirmed { get; set; }
    }
}