using Berryfy.Application.Constants;

namespace Berryfy.Application.Dtos
{
    public class ApplicationResponse<T>
    {
        public bool IsSuccess { get; set; }
        public T? Value { get; set; }
        public string? ErrorMessage { get; set; }
    }
}