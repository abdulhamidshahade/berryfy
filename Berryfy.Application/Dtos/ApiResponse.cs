namespace Berryfy.Application.Dtos
{
    public class ApiResponse<T>
    {
        public bool IsSuccess { get; set; }
        public int StatusCode { get; set; }
        public string? StatusMessage { get; set; }
        public T? Data { get; set; }
        public List<string> Errors { get; set; }
    }
}
