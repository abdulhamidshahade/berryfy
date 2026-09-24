using Berryfy.Application.Authorization.Attributes;
using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.AuthDtos.Requests;
using Berryfy.Application.Dtos.AuthDtos.Responses;
using Berryfy.Application.Services.Interfaces.AuthServiceInterfaces;
using Microsoft.AspNetCore.Mvc;

namespace Berryfy.API.Controllers
{
    [ApiController]
    [Route("api/role-managements")]
    [SuperAdminOnly]
    public class RoleManagementsController : BaseController
    {
        private readonly IRoleManagementService _roleManagementService;
        public RoleManagementsController(IRoleManagementService roleManagementService)
        {
            _roleManagementService = roleManagementService;
        }

        [HttpPost("roles")]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
        {
            var result = await _roleManagementService.CreateRoleAsync(request.roleName);

            if (result.Value)
            {
                return Ok(new ApiResponse<bool>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = $"Role '{request.roleName}' created successfully.",
                    Data = true
                });
            }

            return BadRequest(new ApiResponse<bool>
            {
                IsSuccess = false,
                StatusCode = 400,
                StatusMessage = $"Failed to create role '{request.roleName}'.",
                Data = false
            });
        }

        [HttpDelete("roles/{roleName}")]
        public async Task<IActionResult> DeleteRole(string roleName)
        {
            var result = await _roleManagementService.DeleteRoleAsync(roleName);

            if (result.Value)
            {
                return Ok(new ApiResponse<bool>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = $"Role '{roleName}' deleted successfully.",
                    Data = true
                });
            }

            return BadRequest(new ApiResponse<bool>
            {
                IsSuccess = false,
                StatusCode = 400,
                StatusMessage = $"Failed to delete role '{roleName}'."
            });
        }

        [HttpPost("users/{userId}/roles/{roleName}")]
        public async Task<IActionResult> AssignRoleToUser(int userId, string roleName)
        {
            var result = await _roleManagementService.AssignRoleToUserAsync(userId, roleName);

            if (result.Value)
            {
                return Ok(new ApiResponse<bool>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = $"Role '{roleName}' assigned to user '{userId}' successfully.",
                    Data = true
                });
            }

            return BadRequest(new ApiResponse<bool>
            {
                IsSuccess = false,
                StatusCode = 400,
                StatusMessage = $"Failed to assign role '{roleName}' to user '{userId}'.",
                Data = false
            });
        }

        [HttpDelete("users/{userId}/roles/{roleName}")]
        public async Task<IActionResult> RemoveRoleFromUser(int userId, string roleName)
        {
            var result = await _roleManagementService.RemoveRoleFromUserAsync(userId, roleName);

            if (result.Value)
            {
                return Ok(new ApiResponse<bool>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = $"Role '{roleName}' removed from user '{userId}' successfully.",
                    Data = true
                });
            }

            return BadRequest(new ApiResponse<bool>
            {
                IsSuccess = false,
                StatusCode = 400,
                StatusMessage = $"Failed to remove role '{roleName}' from user {userId}.",
                Data = false
            });
        }

        [HttpGet("users/{userId}/roles")]
        public async Task<IActionResult> GetUserRoles(int userId)
        {
            var roles = await _roleManagementService.GetUserRolesAsync(userId);

            return Ok(new ApiResponse<List<string>>
            {
                IsSuccess = true,
                StatusCode = 200,
                StatusMessage = $"User '{userId}'s roles retrieved successfully.",
                Data = roles.Value
            });
        }

        [HttpGet("roles")]
        [SuperAdminOnly]
        public async Task<IActionResult> GetAllRoles()
        {
            var roles = await _roleManagementService.GetAllRolesAsync();

            return Ok(new ApiResponse<List<RoleResponse>>
            {
                IsSuccess = true,
                StatusCode = 200,
                StatusMessage = "All roles retrieved successfully.",
                Data = roles.Value
            });
        }

        [HttpGet("roles/{roleName}/users")]
        public async Task<IActionResult> GetUsersInRole(string roleName)
        {
            var users = await _roleManagementService.GetUsersInRoleAsync(roleName);

            return Ok(new ApiResponse<List<UserResponse>>
            {
                IsSuccess = true,
                StatusCode = 200,
                StatusMessage = $"Users in role '{roleName}' retrieved successfully.",
                Data = users.Value
            });
        }

        [HttpGet("users/{userId}/roles/{roleName}/check")]
        public async Task<IActionResult> IsUserInRole(int userId, string roleName)
        {
            var isInRole = await _roleManagementService.IsUserInRoleAsync(userId, roleName);

            return Ok(new ApiResponse<bool>
            {
                IsSuccess = true,
                StatusCode = 200,
                StatusMessage = "Role check completed.",
                Data = isInRole.Value
            });
        }

        [HttpPost("initialize-default-roles")]
        public async Task<IActionResult> InitializeDefaultRoles()
        {
            await _roleManagementService.InitializeDefaultRolesAsync();

            return Ok(new ApiResponse<object>
            {
                IsSuccess = true,
                StatusCode = 200,
                StatusMessage = "Default roles initialized successfully.",
                Data = null
            });
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _roleManagementService.GetAllUsersAsync();

            return Ok(new ApiResponse<List<UserWithRolesResponse>>
            {
                IsSuccess = true,
                StatusCode = 200,
                StatusMessage = "All users retrieved successfully.",
                Data = users.Value
            });
        }

        [HttpGet("users/{userId}")]
        public async Task<IActionResult> GetUser(int userId)
        {
            var user = await _roleManagementService.GetUserByIdAsync(userId);

            if (user == null)
            {
                return NotFound(new ApiResponse<UserWithRolesResponse>
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    StatusMessage = $"User with given '{userId}' not found.",
                    Data = null
                });
            }

            return Ok(new ApiResponse<UserWithRolesResponse>
            {
                IsSuccess = true,
                StatusCode = 200,
                StatusMessage = $"User '{userId}' retrieved successfully.",
                Data = user.Value
            });
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetRoleStats()
        {
            var stats = await _roleManagementService.GetRoleStatsAsync();

            return Ok(new ApiResponse<RoleStats>
            {
                IsSuccess = true,
                StatusCode = 200,
                StatusMessage = "Role statistics retrieved successfully.",
                Data = stats.Value
            });
        }

        [HttpPut("roles")]
        public async Task<IActionResult> UpdateRole(UpdateRoleRequest request)
        {
            var result = await _roleManagementService.UpdateRoleAsync(request.oldRoleName, request.newRoleName);

            if (result.Value)
            {
                return Ok(new ApiResponse<bool>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = $"Role '{request.oldRoleName}' updated to '{request.newRoleName}' successfully.",
                    Data = result.Value
                });
            }

            return BadRequest(new ApiResponse<bool>
            {
                IsSuccess = false,
                StatusCode = 400,
                StatusMessage = $"Failed to update role '{request.oldRoleName}'.",
                Data = false
            });
        }

        [HttpPost("bulk-assign")]
        public async Task<IActionResult> BulkAssignRole([FromBody] BulkAssignRoleRequest request)
        {
            if (request == null || request.UserIds == null || !request.UserIds.Any() || string.IsNullOrWhiteSpace(request.RoleName))
            {
                return BadRequest(new ApiResponse<BulkAssignmentResult>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = "Invalid request. User IDs and role name are required."
                });
            }

            var result = await _roleManagementService.BulkAssignRoleAsync(request.UserIds, request.RoleName);

            if (result.IsSuccess)
            {
                return Ok(new ApiResponse<BulkAssignmentResult>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = $"Successfully assigned role '{request.RoleName}' to '{result.Value.SuccessfulAssignments}' users.",
                    Data = result.Value
                });
            }

            return BadRequest(new ApiResponse<BulkAssignmentResult>
            {
                IsSuccess = false,
                StatusCode = 400,
                StatusMessage = $"Bulk assignment partially failed. '{result.Value.SuccessfulAssignments}' succeeded, '{result.Value.FailedAssignments}' failed.",
                Data = result.Value
            });
        }
    }
}