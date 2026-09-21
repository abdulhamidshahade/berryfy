using Berryfy.Application.Dtos.AuthDtos.Responses;
namespace Berryfy.Application.Services.Interfaces.AuthServiceInterfaces
{
    public interface IRoleManagementService
    {
        Task<bool> CreateRoleAsync(string roleName);
        Task<bool> DeleteRoleAsync(string roleName);
        Task<bool> AssignRoleToUserAsync(int userId, string roleName);
        Task<bool> RemoveRoleFromUserAsync(int userId, string roleName);
        Task<List<string>> GetUserRolesAsync(int userId);
        Task<List<RoleResponse>> GetAllRolesAsync();
        Task<List<UserResponse>> GetUsersInRoleAsync(string roleName);
        Task<bool> IsUserInRoleAsync(int userId, string roleName);
        Task InitializeDefaultRolesAsync();
        Task<List<UserWithRolesResponse>> GetAllUsersAsync();
        Task<UserWithRolesResponse> GetUserByIdAsync(int userId);
        Task<RoleStats> GetRoleStatsAsync();
        Task<bool> UpdateRoleAsync(string oldRoleName, string newRoleName);
        Task<BulkAssignmentResult> BulkAssignRoleAsync(List<int> userIds, string roleName);
    }
}
