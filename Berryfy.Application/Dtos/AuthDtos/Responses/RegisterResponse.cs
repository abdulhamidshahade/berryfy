namespace Berryfy.Application.Dtos.AuthDtos.Responses
{
    public class RegisterResponse
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public UserResponse User { get; set; }
    }
}
