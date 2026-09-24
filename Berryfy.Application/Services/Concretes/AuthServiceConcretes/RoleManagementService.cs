using Berryfy.Application.Dtos;
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

        public async Task<ApplicationResponse<bool>> CreateRoleAsync(string roleName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(roleName))
                {
                    return new ApplicationResponse<bool>()
                    {
                        IsSuccess = false,
                        Value = false,
                        ErrorMessage = "The role name is null or empty"
                    };
                }

                if (_roleRepository.RoleExistsAsync(roleName).GetAwaiter().GetResult().Value)
                {
                    return new ApplicationResponse<bool>()
                    {
                        IsSuccess = false,
                        Value = false,
                        ErrorMessage = $"The role {roleName} exists"
                    };
                }

                await _roleRepository.CreateAsync(new Role(roleName.Trim()));

                return new ApplicationResponse<bool>()
                {
                    IsSuccess = true,
                    Value = true,
                    SuccessMessage = $"Role \'{roleName}\' created successfully."
                };
            }
            catch (Exception ex)
            {
                return new ApplicationResponse<bool>()
                {
                    IsSuccess = false,
                    Value = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<ApplicationResponse<bool>> DeleteRoleAsync(string roleName)
        {
            try
            {
                if (IsProtectedRole(roleName))
                {
                    return new ApplicationResponse<bool>()
                    {
                        IsSuccess = false,
                        Value = false,
                        ErrorMessage = $"Attempt to delete system role \'{roleName}\' denied."
                    };
                }

                var usersInRole = await _roleRepository.GetUsersInRoleAsync(roleName);

                if (usersInRole.Value.Any())
                {
                    return new ApplicationResponse<bool>()
                    {
                        IsSuccess = false,
                        Value = false,
                        ErrorMessage = $"Cannot delete role \'{roleName}\' because it is assigned to {usersInRole.Value.Count} user(s)."
                    };
                }

                return new ApplicationResponse<bool>()
                {
                    IsSuccess = true,
                    Value = _roleRepository.DeleteAsync(roleName).GetAwaiter().GetResult().Value,
                    SuccessMessage = "The role deleted successfully"
                };
            }
            catch (Exception ex)
            {
                return new ApplicationResponse<bool>()
                {
                    IsSuccess = false,
                    Value = false,
                    SuccessMessage = $"Error deleting role \'{roleName}\'"
                };
            }
        }

        public async Task<ApplicationResponse<bool>> AssignRoleToUserAsync(int userId, string roleName)
        {
            try
            {
                if (!await _userRepository.ExistsByIdAsync(userId))
                {
                    return new ApplicationResponse<bool>()
                    {
                        IsSuccess = false,
                        Value = false,
                        ErrorMessage = $"The user is not exists by given the id: {userId}"
                    };   
                }

                if (!_roleRepository.RoleExistsAsync(roleName).GetAwaiter().GetResult().Value)
                {
                    await CreateRoleAsync(roleName);
                }

                if (_roleRepository.IsUserInRoleAsync(userId, roleName).GetAwaiter().GetResult().Value)
                {
                    return new ApplicationResponse<bool>()
                    {
                        IsSuccess = false,
                        Value = false,
                        ErrorMessage = "The user is already in the role"
                    };
                }

                return new ApplicationResponse<bool>()
                {
                    IsSuccess = true,
                    Value = _roleRepository.AssignRoleToUserAsync(userId, roleName).GetAwaiter().GetResult().Value,
                    SuccessMessage = $"The role -{roleName}- assigned to the user with id -{userId}- successfully"
                };
            }
            catch (Exception ex)
            {
                return new ApplicationResponse<bool>()
                {
                    IsSuccess = false,
                    Value = false,
                    ErrorMessage = $"{ex.Message} - Error assigning role \'{roleName}\' to user with ID \'{userId}\'"
                };
            }
        }

        public async Task<ApplicationResponse<bool>> RemoveRoleFromUserAsync(int userId, string roleName)
        {
            try
            {
                if (!_roleRepository.IsUserInRoleAsync(userId, roleName).GetAwaiter().GetResult().Value)
                {
                    return new ApplicationResponse<bool>()
                    {
                        IsSuccess = false,
                        Value = false,
                        ErrorMessage = $"The user: {userId} is not assigned with the role: {roleName}"
                    };
                }

                if (string.Equals(roleName, RoleConstants.SuperAdmin, StringComparison.OrdinalIgnoreCase))
                {
                    var superAdmins = await _roleRepository.GetUsersInRoleAsync(RoleConstants.SuperAdmin);
                    if (superAdmins.Value.Count <= 1)
                    {
                        return new ApplicationResponse<bool>()
                        {
                            IsSuccess = false,
                            Value = false,
                            ErrorMessage = "Cannot remove SuperAdmin role from the last SuperAdmin user."
                        };
                    }
                }

                return new ApplicationResponse<bool>()
                {
                    IsSuccess = true,
                    Value = _roleRepository.RemoveRoleFromUserAsync(userId, roleName).GetAwaiter().GetResult().Value,
                    ErrorMessage = $"The role: {roleName} is removed from user: {userId}"
                };
            }
            catch (Exception ex)
            {
                return new ApplicationResponse<bool>()
                {
                    IsSuccess = false,
                    Value = false,
                    ErrorMessage = $"{ex.Message} - Error removign role \'{roleName}\' from user: {userId}"
                };
            }
        }

        public async Task<ApplicationResponse<List<string>>> GetUserRolesAsync(int userId)
        {
            try
            {
                if (!await _userRepository.ExistsByIdAsync(userId))
                {
                    return new ApplicationResponse<List<string>>()
                    {
                        IsSuccess = false,
                        ErrorMessage = $"No roles found with user: \'{userId}\'"
                    };
                }

                return new ApplicationResponse<List<string>>()
                {
                    IsSuccess = true,
                    SuccessMessage = "The roles for userId retrived successfully!",
                    Value = _roleRepository.GetUserRolesAsync(userId).GetAwaiter().GetResult().Value
                };
            }
            catch (Exception ex)
            {
                return new ApplicationResponse<List<string>>()
                {
                    IsSuccess = false,
                    ErrorMessage = $"{ex.Message} - Error getting roles for user with user \'{userId}\'"
                };
            }
        }

        public async Task<ApplicationResponse<List<RoleResponse>>> GetAllRolesAsync()
        {
            try
            {
                return new ApplicationResponse<List<RoleResponse>>()
                {
                    IsSuccess = true,
                    SuccessMessage = "All roles retrived successfully from database!",
                    Value = RoleResponse.MapFromRole(_roleRepository.GetAllAsync().GetAwaiter().GetResult().Value)
                };
            }
            catch (Exception ex)
            {
                return new ApplicationResponse<List<RoleResponse>>()
                {
                    IsSuccess = false,
                    ErrorMessage = $"{ex.Message} - Error while getting all roles!"
                };
            }
        }

        public async Task<ApplicationResponse<List<UserResponse>>> GetUsersInRoleAsync(string roleName)
        {
            try
            {
                var users = await _roleRepository.GetUsersInRoleAsync(roleName);
                return new ApplicationResponse<List<UserResponse>>()
                {
                    IsSuccess = true,
                    SuccessMessage = $"All users retrived of role: {roleName}",
                    Value = users.Value.Select(user => UserResponse.MapFromUser(user, new[] { roleName })).ToList()
                };
            }
            catch (Exception ex)
            {
                return new ApplicationResponse<List<UserResponse>>()
                {
                    IsSuccess = false,
                    ErrorMessage = $"{ex.Message} - Error getting users in role \'{roleName}\'"
                };
            }
        }

        public async Task<ApplicationResponse<bool>> IsUserInRoleAsync(int userId, string roleName)
        {
            return new ApplicationResponse<bool>()
            {
                IsSuccess = true,
                Value = _roleRepository.IsUserInRoleAsync(userId, roleName).GetAwaiter().GetResult().Value
            };
        }

        public async Task InitializeDefaultRolesAsync()
        {
            foreach (var role in RoleConstants.AllRoles)
            {
                await CreateRoleAsync(role);
            }

            _logger.LogInformation("Default roles initialized successfully.");
        }

        public async Task<ApplicationResponse<List<UserWithRolesResponse>>> GetAllUsersAsync()
        {
            try
            {
                var users = await _roleRepository.GetAllUsersWithRolesAsync();
                return new ApplicationResponse<List<UserWithRolesResponse>>()
                {
                    IsSuccess = true,
                    Value = users.Value
                    .Select(entry => UserWithRolesResponse.MapFromUser(entry.User, entry.Roles))
                    .ToList()
                };
            }
            catch (Exception ex)
            {
                return new ApplicationResponse<List<UserWithRolesResponse>>()
                {
                    IsSuccess = false,
                    ErrorMessage = $"{ex.Message} - Error getting all users with their related roles"
                };
            }
        }

        public async Task<ApplicationResponse<UserWithRolesResponse>> GetUserByIdAsync(int userId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return null;
                }

                var roles = await _roleRepository.GetUserRolesAsync(user.Id);
                return new ApplicationResponse<UserWithRolesResponse>()
                {
                    IsSuccess = true,
                    Value = UserWithRolesResponse.MapFromUser(user, roles.Value)
                };
            }
            catch (Exception ex)
            {
                return new ApplicationResponse<UserWithRolesResponse>()
                {
                    IsSuccess = false,
                    ErrorMessage = $"{ex.Message} - Error while getting the user with his/her related roles"
                };
            }
        }

        public async Task<ApplicationResponse<RoleStats>> GetRoleStatsAsync()
        {
            try
            {
                var result = await _roleRepository.GetRoleStatsAsync();
                return new ApplicationResponse<RoleStats>()
                {
                    IsSuccess = true,
                    Value = new RoleStats
                    {
                        TotalRoles = result.Value.TotalRoles,
                        TotalUsers = result.Value.TotalUsers,
                        UsersWithRoles = result.Value.UsersWithRoles,
                        UsersWithoutRoles = result.Value.UsersWithoutRoles
                    }
                };
            }
            catch (Exception ex)
            {
                return new ApplicationResponse<RoleStats>()
                {
                    IsSuccess = false,
                    ErrorMessage = $"{ex.Message} - Error getting role statistics"
                };
            }
        }

        public async Task<ApplicationResponse<bool>> UpdateRoleAsync(string oldRoleName, string newRoleName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(oldRoleName) || string.IsNullOrWhiteSpace(newRoleName))
                {
                    return new ApplicationResponse<bool>()
                    {
                        IsSuccess = false,
                        ErrorMessage = "new/old role name empty or null"
                    };
                }

                if (IsProtectedRole(oldRoleName))
                {
                    return new ApplicationResponse<bool>()
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Attempt to update system role '{oldRoleName}' denied."
                    };
                }

                if (!_roleRepository.RoleExistsAsync(oldRoleName).GetAwaiter().GetResult().Value ||
                     _roleRepository.RoleExistsAsync(newRoleName).GetAwaiter().GetResult().Value)
                {
                    return new ApplicationResponse<bool>()
                    {
                        IsSuccess = false,
                        ErrorMessage = $"{oldRoleName}/{newRoleName} don't exists!"
                    };
                }

                return new ApplicationResponse<bool>()
                {
                    IsSuccess = true,
                    Value = _roleRepository.UpdateAsync(oldRoleName, newRoleName).GetAwaiter().GetResult().Value
                };
            }
            catch (Exception ex)
            {
                return new ApplicationResponse<bool>()
                {
                    IsSuccess = false,
                    ErrorMessage = $"{ex.Message} - Error updating role \'{oldRoleName}\' to \'{newRoleName}\'"
                };
            }
        }

        public async Task<ApplicationResponse<BulkAssignmentResult>> BulkAssignRoleAsync(List<int> userIds, string roleName)
        {
            var result = new BulkAssignmentResult
            {
                TotalUsers = userIds?.Count ?? 0
            };

            if (userIds == null || !userIds.Any() || string.IsNullOrWhiteSpace(roleName))
            {
                result.ErrorMessages.Add("Invalid input parameters");
                return new ApplicationResponse<BulkAssignmentResult>()
                {
                    IsSuccess = false,
                    Value = result
                };
            }

            if (!_roleRepository.RoleExistsAsync(roleName).GetAwaiter().GetResult().Value)
            {
                await CreateRoleAsync(roleName);
            }

            foreach (var userId in userIds)
            {
                try
                {
                    if (AssignRoleToUserAsync(userId, roleName).GetAwaiter().GetResult().Value)
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

            return new ApplicationResponse<BulkAssignmentResult>()
            {
                IsSuccess = true,
                Value = result
            };
        }

        private static bool IsProtectedRole(string roleName)
        {
            return string.Equals(roleName, RoleConstants.SuperAdmin, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(roleName, RoleConstants.Admin, StringComparison.OrdinalIgnoreCase);
        }
    }
}
