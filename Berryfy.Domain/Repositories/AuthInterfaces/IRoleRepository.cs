using Berryfy.Domain.Entities.AuthEntities;

namespace Berryfy.Domain.Repositories.AuthInterfaces
{
    public interface IRoleRepository
    {
        Task<bool> RoleExistsAsync(string roleName);
        Task<Role?> GetByNameAsync(string roleName);
        Task<Role> CreateAsync(Role role);
        Task<bool> DeleteAsync(string roleName);
        Task<bool> UpdateAsync(string oldRoleName, string newRoleName);
        Task<List<Role>> GetAllAsync();
        Task<bool> AssignRoleToUserAsync(int userId, string roleName);
        Task<bool> RemoveRoleFromUserAsync(int userId, string roleName);
        Task<bool> IsUserInRoleAsync(int userId, string roleName);
        Task<List<string>> GetUserRolesAsync(int userId);
        Task<List<User>> GetUsersInRoleAsync(string roleName);
        Task<List<(User User, List<string> Roles)>> GetAllUsersWithRolesAsync();
        Task<(int TotalRoles, int TotalUsers, int UsersWithRoles, int UsersWithoutRoles)> GetRoleStatsAsync();
    }
}
