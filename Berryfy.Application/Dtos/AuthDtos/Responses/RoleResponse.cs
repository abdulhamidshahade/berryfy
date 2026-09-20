namespace Berryfy.Application.Dtos.AuthDtos.Responses
{
    public class RoleResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string concurrencyStamp { get; set; }
        public string normalizedName { get; set; }
    }
}
