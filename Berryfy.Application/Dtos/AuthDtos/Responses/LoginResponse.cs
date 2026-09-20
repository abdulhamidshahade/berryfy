namespace Berryfy.Application.Dtos.AuthDtos.Responses
{
    public class LoginResponse
    {
        public UserResponse User { get; set; }
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public string ErrorMessage { get; set; }
    }
}
