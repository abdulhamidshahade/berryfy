using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.AuthDtos.Responses;
namespace Berryfy.Application.Services.Interfaces.AuthServiceInterfaces
{
    public interface IRoleManagementService
    {
        Task<ApplicationResponse<bool>> CreateRoleAsync(string roleName);
        Task<ApplicationResponse<bool>> DeleteRoleAsync(string roleName);
        Task<ApplicationResponse<bool>> AssignRoleToUserAsync(int userId, string roleName);
        Task<ApplicationResponse<bool>> RemoveRoleFromUserAsync(int userId, string roleName);
        Task<ApplicationResponse<List<string>>> GetUserRolesAsync(int userId);
        Task<ApplicationResponse<List<RoleResponse>>> GetAllRolesAsync();
        Task<ApplicationResponse<List<UserResponse>>> GetUsersInRoleAsync(string roleName);
        Task<ApplicationResponse<bool>> IsUserInRoleAsync(int userId, string roleName);
        Task InitializeDefaultRolesAsync();
        Task<ApplicationResponse<List<UserWithRolesResponse>>> GetAllUsersAsync();
        Task<ApplicationResponse<UserWithRolesResponse>> GetUserByIdAsync(int userId);
        Task<ApplicationResponse<RoleStats>> GetRoleStatsAsync();
        Task<ApplicationResponse<bool>> UpdateRoleAsync(string oldRoleName, string newRoleName);
        Task<ApplicationResponse<BulkAssignmentResult>> BulkAssignRoleAsync(List<int> userIds, string roleName);
    }
}