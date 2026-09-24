using Berryfy.Domain.Entities;
using Berryfy.Domain.Entities.AuthEntities;

namespace Berryfy.Domain.Repositories.AuthInterfaces
{
    public interface IRoleRepository
    {
        Task<InfrastructureResponse<bool>> RoleExistsAsync(string roleName);
        Task<InfrastructureResponse<Role?>> GetByNameAsync(string roleName);
        Task<InfrastructureResponse<Role>> CreateAsync(Role role);
        Task<InfrastructureResponse<bool>> DeleteAsync(string roleName);
        Task<InfrastructureResponse<bool>> UpdateAsync(string oldRoleName, string newRoleName);
        Task<InfrastructureResponse<List<Role>>> GetAllAsync();
        Task<InfrastructureResponse<bool>> AssignRoleToUserAsync(int userId, string roleName);
        Task<InfrastructureResponse<bool>> RemoveRoleFromUserAsync(int userId, string roleName);
        Task<InfrastructureResponse<bool>> IsUserInRoleAsync(int userId, string roleName);
        Task<InfrastructureResponse<List<string>>> GetUserRolesAsync(int userId);
        Task<InfrastructureResponse<List<User>>> GetUsersInRoleAsync(string roleName);
        Task<InfrastructureResponse<List<(User User, List<string> Roles)>>> GetAllUsersWithRolesAsync();
        Task<InfrastructureResponse<(int TotalRoles, int TotalUsers, int UsersWithRoles, int UsersWithoutRoles)>> GetRoleStatsAsync();
    }
}
