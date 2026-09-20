namespace Berryfy.Application.Dtos.AuthDtos.Responses
{
    public class RoleStats
    {
        public int TotalRoles { get; set; }
        public int TotalUsers { get; set; }
        public int UsersWithRoles { get; set; }
        public int UsersWithoutRoles { get; set; }
    }
}
