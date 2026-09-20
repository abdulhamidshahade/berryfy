namespace Berryfy.Application.Dtos.AuthDtos.Requests
{
    public class BulkAssignRoleRequest
    {
        public List<int> UserIds { get; set; }
        public string RoleName { get; set; }
    }
}
