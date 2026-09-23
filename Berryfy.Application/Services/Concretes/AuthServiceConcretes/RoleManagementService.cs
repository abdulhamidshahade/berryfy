using Berryfy.Application.Dtos.AuthDtos.Responses;
using Berryfy.Application.Services.Interfaces.AuthServiceInterfaces;
using Berryfy.Domain.Constants;
using Berryfy.Domain.Entities.AuthEntities;
using Berryfy.Domain.Repositories.AuthInterfaces;
using Microsoft.Extensions.Logging;

namespace Berryfy.Application.Services.Concretes.AuthServiceConcretes
{
    public class RoleManagementService : IRoleManagementService
    {
        private readonly ILogger<RoleManagementService> _logger;
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;

        public RoleManagementService(
            ILogger<RoleManagementService> logger,
            IUserRepository userRepository,
            IRoleRepository roleRepository)
        {
            _logger = logger;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
        }

        public async Task<bool> CreateRoleAsync(string roleName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(roleName))
                {
                    return false;
                }

                if (await _roleRepository.RoleExistsAsync(roleName))
                {
                    return true;
                }

                await _roleRepository.CreateAsync(new Role(roleName.Trim()));
                _logger.LogInformation("Role '{RoleName}' created successfully.", roleName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating role '{RoleName}'", roleName);
                return false;
            }
        }

        public async Task<bool> DeleteRoleAsync(string roleName)
        {
            try
            {
                if (IsProtectedRole(roleName))
                {
                    _logger.LogWarning("Attempt to delete system role '{RoleName}' denied.", roleName);
                    return false;
                }

                var usersInRole = await _roleRepository.GetUsersInRoleAsync(roleName);
                if (usersInRole.Any())
                {
                    _logger.LogWarning("Cannot delete role '{RoleName}' because it is assigned to {UserCount} user(s).", roleName, usersInRole.Count);
                    return false;
                }

                return await _roleRepository.DeleteAsync(roleName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting role '{RoleName}'", roleName);
                return false;
            }
        }

        public async Task<bool> AssignRoleToUserAsync(int userId, string roleName)
        {
            try
            {
                if (!await _userRepository.ExistsByIdAsync(userId))
                {
                    return false;
                }

                if (!await _roleRepository.RoleExistsAsync(roleName))
                {
                    await CreateRoleAsync(roleName);
                }

                if (await _roleRepository.IsUserInRoleAsync(userId, roleName))
                {
                    return true;
                }

                return await _roleRepository.AssignRoleToUserAsync(userId, roleName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role '{RoleName}' to user with ID '{UserId}'", roleName, userId);
                return false;
            }
        }

        public async Task<bool> RemoveRoleFromUserAsync(int userId, string roleName)
        {
            try
            {
                if (!await _roleRepository.IsUserInRoleAsync(userId, roleName))
                {
                    return true;
                }

                if (string.Equals(roleName, RoleConstants.SuperAdmin, StringComparison.OrdinalIgnoreCase))
                {
                    var superAdmins = await _roleRepository.GetUsersInRoleAsync(RoleConstants.SuperAdmin);
                    if (superAdmins.Count <= 1)
                    {
                        _logger.LogWarning("Cannot remove SuperAdmin role from the last SuperAdmin user.");
                        return false;
                    }
                }

                return await _roleRepository.RemoveRoleFromUserAsync(userId, roleName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing role '{RoleName}' from user with ID '{UserId}'", roleName, userId);
                return false;
            }
        }

        public async Task<List<string>> GetUserRolesAsync(int userId)
        {
            try
            {
                if (!await _userRepository.ExistsByIdAsync(userId))
                {
                    return new List<string>();
                }

                return await _roleRepository.GetUserRolesAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roles for user with ID '{UserId}'", userId);
                return new List<string>();
            }
        }

        public async Task<List<RoleResponse>> GetAllRolesAsync()
        {
            try
            {
                return RoleResponse.MapFromRole(await _roleRepository.GetAllAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all roles");
                return new List<RoleResponse>();
            }
        }

        public async Task<List<UserResponse>> GetUsersInRoleAsync(string roleName)
        {
            try
            {
                var users = await _roleRepository.GetUsersInRoleAsync(roleName);
                return users.Select(user => UserResponse.MapFromUser(user, new[] { roleName })).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users in role '{RoleName}'", roleName);
                return new List<UserResponse>();
            }
        }

        public Task<bool> IsUserInRoleAsync(int userId, string roleName)
        {
            return _roleRepository.IsUserInRoleAsync(userId, roleName);
        }

        public async Task InitializeDefaultRolesAsync()
        {
            foreach (var role in RoleConstants.AllRoles)
            {
                await CreateRoleAsync(role);
            }

            _logger.LogInformation("Default roles initialized successfully.");
        }

        public async Task<List<UserWithRolesResponse>> GetAllUsersAsync()
        {
            try
            {
                var users = await _roleRepository.GetAllUsersWithRolesAsync();
                return users
                    .Select(entry => UserWithRolesResponse.MapFromUser(entry.User, entry.Roles))
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                return new List<UserWithRolesResponse>();
            }
        }

        public async Task<UserWithRolesResponse> GetUserByIdAsync(int userId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return null;
                }

                var roles = await _roleRepository.GetUserRolesAsync(user.Id);
                return UserWithRolesResponse.MapFromUser(user, roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user with ID '{UserId}'", userId);
                return null;
            }
        }

        public async Task<RoleStats> GetRoleStatsAsync()
        {
            try
            {
                var result = await _roleRepository.GetRoleStatsAsync();
                return new RoleStats
                {
                    TotalRoles = result.TotalRoles,
                    TotalUsers = result.TotalUsers,
                    UsersWithRoles = result.UsersWithRoles,
                    UsersWithoutRoles = result.UsersWithoutRoles
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting role statistics");
                return new RoleStats();
            }
        }

        public async Task<bool> UpdateRoleAsync(string oldRoleName, string newRoleName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(oldRoleName) || string.IsNullOrWhiteSpace(newRoleName))
                {
                    return false;
                }

                if (IsProtectedRole(oldRoleName))
                {
                    _logger.LogWarning("Attempt to update system role '{RoleName}' denied.", oldRoleName);
                    return false;
                }

                if (!await _roleRepository.RoleExistsAsync(oldRoleName) ||
                    await _roleRepository.RoleExistsAsync(newRoleName))
                {
                    return false;
                }

                return await _roleRepository.UpdateAsync(oldRoleName, newRoleName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role '{OldRoleName}' to '{NewRoleName}'", oldRoleName, newRoleName);
                return false;
            }
        }

        public async Task<BulkAssignmentResult> BulkAssignRoleAsync(List<int> userIds, string roleName)
        {
            var result = new BulkAssignmentResult
            {
                TotalUsers = userIds?.Count ?? 0
            };

            if (userIds == null || !userIds.Any() || string.IsNullOrWhiteSpace(roleName))
            {
                result.ErrorMessages.Add("Invalid input parameters");
                return result;
            }

            if (!await _roleRepository.RoleExistsAsync(roleName))
            {
                await CreateRoleAsync(roleName);
            }

            foreach (var userId in userIds)
            {
                try
                {
                    if (await AssignRoleToUserAsync(userId, roleName))
                    {
                        result.SuccessfulAssignments++;
                    }
                    else
                    {
                        result.FailedAssignments++;
                        result.FailedUserIds.Add(userId.ToString());
                        result.ErrorMessages.Add($"Failed to assign role to user {userId}");
                    }
                }
                catch (Exception ex)
                {
                    result.FailedAssignments++;
                    result.FailedUserIds.Add(userId.ToString());
                    result.ErrorMessages.Add($"Error assigning role to user {userId}: {ex.Message}");
                    _logger.LogError(ex, "Error in bulk assignment for user {UserId}", userId);
                }
            }

            return result;
        }

        private static bool IsProtectedRole(string roleName)
        {
            return string.Equals(roleName, RoleConstants.SuperAdmin, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(roleName, RoleConstants.Admin, StringComparison.OrdinalIgnoreCase);
        }
    }
}
