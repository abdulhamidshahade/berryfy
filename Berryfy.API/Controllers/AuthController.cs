using Berryfy.Application.Authorization.Attributes;
using Berryfy.Application.Constants;
using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.AuthDtos.Requests;
using Berryfy.Application.Dtos.AuthDtos.Responses;
using Berryfy.Application.Services.Interfaces.AuthServiceInterfaces;
using Berryfy.Application.Services.Interfaces.ShoppingCartServiceInterfaces;
using Berryfy.Domain.Entities.AuthEntities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Berryfy.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : BaseController
    {
        private readonly IAuthService _authService;
        private readonly IUserService _userService;
        private readonly ICartService _cartService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService,
                              IUserService userService,
                              ICartService cartService,
                              ILogger<AuthController> logger)
        {
            _authService = authService;
            _userService = userService;
            _cartService = cartService;
            _logger = logger;
        }

        [HttpPost]
        [Route("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest requestDto)
        {
            if (requestDto == null || !ModelState.IsValid)
            {
                return BadRequest(new ResponseDto<RegisterResponse>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = "Invalid registration data.",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var registerResult = await _authService.Register(requestDto);

            switch (registerResult)
            {
                case Result<RegisterResponse> success when success.Status == ResultStatus.Success:
                {
                    var newUserId = success.Value?.User?.Id ?? 0;
                    if (newUserId > 0)
                    {
                        var sessionId = GetSessionId();
                        if (!string.IsNullOrWhiteSpace(sessionId))
                        {
                            await _cartService.MergeCartAsync(newUserId, sessionId);
                        }
                    }

                    return StatusCode(201, new ResponseDto<RegisterResponse>
                    {
                        IsSuccess = true,
                        StatusCode = 201,
                        StatusMessage = "User registered successfully. Please check your email to confirm your account.",
                        Data = success.Value
                    });
                }

                case Result<RegisterResponse> validationError when validationError.Status == ResultStatus.ValidationError:
                    return BadRequest(new ResponseDto<RegisterResponse>
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        StatusMessage = validationError.Error,
                    });

                case Result<RegisterResponse> failure when failure.Status == ResultStatus.Failure:
                    return StatusCode(500, new ResponseDto<RegisterResponse>
                    {
                        IsSuccess = false,
                        StatusCode = 500,
                        StatusMessage = failure.Error,
                    });
                case Result<RegisterResponse> conflict when conflict.Status == ResultStatus.Conflict:
                    return Conflict(new ResponseDto<RegisterResponse>
                    {
                        IsSuccess = false,
                        StatusCode = 409,
                        StatusMessage = conflict.Error,
                    });
                case Result<RegisterResponse> verificationRequired when verificationRequired.Status == ResultStatus.VerificationRequired:
                    return StatusCode(403, new ResponseDto<RegisterResponse>
                    {
                        IsSuccess = false,
                        StatusCode = 403,
                        StatusMessage = verificationRequired.Error,
                    });
                default:
                    return Result<RegisterResponse>.NoContent() is Result<RegisterResponse> noContent
                        ? StatusCode(204, new ResponseDto<RegisterResponse>
                        {
                            IsSuccess = false,
                            StatusCode = 204,
                            StatusMessage = "No content to return."
                        })
                        : StatusCode(500, new ResponseDto<RegisterResponse>
                        {
                            IsSuccess = false,
                            StatusCode = 500,
                            StatusMessage = "An unexpected error occurred."
                        });
            }
        }

        [HttpPost]
        [Route("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginRequest requestDto)
        {
            if (requestDto == null || !ModelState.IsValid)
            {
                return StatusCode(400, new ResponseDto<LoginResponse>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = "Invalid request data.",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var loginResult = await _authService.Login(requestDto);

            if (loginResult.Token != string.Empty)
            {
                var user = await _userService.GetUserByEmail(requestDto.Email);
                if (user != null)
                {
                    var sessionId = GetSessionId();
                    if (!string.IsNullOrWhiteSpace(sessionId))
                    {
                        await _cartService.MergeCartAsync(user.Id, sessionId);
                    }
                }

                return StatusCode(200, new ResponseDto<LoginResponse>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = "User logged in successfully.",
                    Data = loginResult
                });
            }

            if (!string.IsNullOrEmpty(loginResult.ErrorMessage))
            {
                return StatusCode(403, new ResponseDto<LoginResponse>
                {
                    IsSuccess = false,
                    StatusCode = 403,
                    StatusMessage = loginResult.ErrorMessage,
                });
            }

            return Unauthorized(new ResponseDto<LoginResponse>
            {
                IsSuccess = false,
                StatusCode = 401,
                StatusMessage = "Invalid email or password",
            });
        }

        [HttpPost]
        [Route("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest requestDto)
        {
            if (requestDto == null || !ModelState.IsValid)
            {
                return StatusCode(400, new ResponseDto<object>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = "Invalid request data.",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            try
            {
                var result = await _authService.ForgotPasswordAsync(requestDto);

                if (result)
                {
                    return Ok(new ResponseDto<object>
                    {
                        IsSuccess = true,
                        StatusCode = 200,
                        StatusMessage = "If the email address exists in our system, you will receive a 6-digit password reset code."
                    });
                }

                return StatusCode(500, new ResponseDto<object>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "An error occurred while processing your request."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<object>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "An error occurred while processing your request."
                });
            }
        }

        [HttpPost]
        [Route("verify-password-reset-code")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyPasswordResetCode([FromBody] EmailConfirmation requestDto)
        {
            if (requestDto == null || !ModelState.IsValid)
            {
                return StatusCode(400, new ResponseDto<VerifyPasswordResetCodeResponse>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = "Invalid request data.",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            try
            {
                var result = await _authService.VerifyPasswordResetCodeAsync(requestDto);

                if (result != null && !string.IsNullOrEmpty(result.ResetToken))
                {
                    return Ok(new ResponseDto<VerifyPasswordResetCodeResponse>
                    {
                        IsSuccess = true,
                        StatusCode = 200,
                        StatusMessage = "Code verified. You can now set a new password.",
                        Data = result
                    });
                }

                return BadRequest(new ResponseDto<VerifyPasswordResetCodeResponse>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = "Invalid or expired code. Please try again or request a new code."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<VerifyPasswordResetCodeResponse>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "An error occurred while verifying your code."
                });
            }
        }

        [HttpPost]
        [Route("resend-password-reset")]
        [AllowAnonymous]
        public async Task<IActionResult> ResendPasswordReset([FromBody] ForgotPasswordRequest requestDto)
        {
            if (requestDto == null || !ModelState.IsValid)
            {
                return StatusCode(400, new ResponseDto<object>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = "Invalid request data.",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            try
            {
                var result = await _authService.ResendPasswordResetCodeAsync(requestDto.Email);

                if (result)
                {
                    return Ok(new ResponseDto<object>
                    {
                        IsSuccess = true,
                        StatusCode = 200,
                        StatusMessage = "If the email address exists in our system, a new reset code has been sent."
                    });
                }

                return StatusCode(500, new ResponseDto<object>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "An error occurred while sending the reset code."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<object>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "An error occurred while sending the reset code."
                });
            }
        }

        [HttpPost]
        [Route("confirm-email")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail([FromBody] EmailConfirmation requestDto)
        {
            if (requestDto == null || !ModelState.IsValid)
            {
                return StatusCode(400, new ResponseDto<object>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = "Invalid request data.",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            try
            {
                var result = await _authService.ConfirmEmailAsync(requestDto);

                if (result)
                {
                    return Ok(new ResponseDto<object>
                    {
                        IsSuccess = true,
                        StatusCode = 200,
                        StatusMessage = "Email confirmed successfully. You can now sign in to your account."
                    });
                }

                return BadRequest(new ResponseDto<object>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = "Failed to confirm email. The confirmation link may be invalid or expired."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<object>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "An error occurred while confirming your email."
                });
            }
        }

        [HttpPost]
        [Route("resend-confirmation")]
        [AllowAnonymous]
        public async Task<IActionResult> ResendConfirmation([FromBody] ForgotPasswordRequest requestDto)
        {
            if (requestDto == null || !ModelState.IsValid)
            {
                return StatusCode(400, new ResponseDto<object>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = "Invalid request data.",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            try
            {
                var result = await _authService.ResendConfirmationEmailAsync(requestDto.Email);

                if (result)
                {
                    return Ok(new ResponseDto<object>
                    {
                        IsSuccess = true,
                        StatusCode = 200,
                        StatusMessage = "If the email address exists in our system, a new confirmation link has been sent."
                    });
                }

                return StatusCode(500, new ResponseDto<object>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "An error occurred while sending the confirmation email."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<object>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "An error occurred while sending the confirmation email."
                });
            }
        }

        [HttpPost]
        [Route("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest requestDto)
        {
            if (requestDto == null || !ModelState.IsValid)
            {
                return StatusCode(400, new ResponseDto<object>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = "Invalid request data.",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            try
            {
                var result = await _authService.ResetPasswordAsync(requestDto);

                if (result)
                {
                    return Ok(new ResponseDto<object>
                    {
                        IsSuccess = true,
                        StatusCode = 200,
                        StatusMessage = "Password has been reset successfully."
                    });
                }

                return BadRequest(new ResponseDto<object>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = "Failed to reset password. Please check your email and reset token."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<object>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "An error occurred while processing your request."
                });
            }
        }

        [HttpGet]
        [AdminAndAbove]
        [Route("exists/{id}")]
        public async Task<ActionResult<ResponseDto<object>>> IsUserExistsById(int id)
        {
            var exists = await _userService.IsUserExistsByIdAsync(id);

            if (!exists)
            {
                return NotFound(new ResponseDto<object>
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    StatusMessage = "The user is not exists by Id"
                });
            }

            return Ok(new ResponseDto<object>
            {
                IsSuccess = true,
                StatusCode = 200,
                StatusMessage = "The user is exists by Id"
            });
        }


        [HttpGet]
        [AllowAnonymous]
        [Route("exists/email-address/{emailAddress}")]
        public async Task<IActionResult> IsUserExistsByEmail(string emailAddress)
        {
            var exists = await _userService.IsUserExistsByEmailAsync(emailAddress);

            if (!exists)
            {
                return NotFound(new ResponseDto<object>
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    StatusMessage = "The user is not exists by Email"
                });
            }

            return Ok(new ResponseDto<object>
            {
                IsSuccess = true,
                StatusCode = 200,
                StatusMessage = "The user is exists by Email"
            });
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("exists/username/{username}")]
        public async Task<IActionResult> IsUserExistsByUsername(string username)
        {
            var exists = await _userService.IsUsernameTaken(username);

            if (!exists)
            {
                return NotFound(new ResponseDto<object>
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    StatusMessage = "The user is not exists by Username"
                });
            }

            return Ok(new ResponseDto<object>
            {
                IsSuccess = true,
                StatusCode = 200,
                StatusMessage = "The user is exists by Username"
            });
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest requestDto)
        {
            if (requestDto == null || string.IsNullOrWhiteSpace(requestDto.Token))
            {
                return BadRequest(new ResponseDto<object>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = "Invalid token provided."
                });
            }

            try
            {
                var result = await _authService.RefreshTokenAsync(requestDto.Token);

                if (string.IsNullOrEmpty(result.Token))
                {
                    return Unauthorized(new ResponseDto<object>
                    {
                        IsSuccess = false,
                        StatusCode = 401,
                        StatusMessage = result.ErrorMessage ?? "Invalid or expired refresh token."
                    });
                }

                return Ok(new ResponseDto<LoginResponse>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = "Token refreshed successfully.",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while refreshing the token.");
                return StatusCode(500, new ResponseDto<object>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "An error occurred while refreshing the token."
                });
            }
        }

        [HttpPost]
        [Route("logout")]
        [AllRoles]
        public async Task<IActionResult> Logout()
        {
            //TODO: implement token blacklisting or session management
            return Ok(new ResponseDto<object>
            {
                IsSuccess = true,
                StatusCode = 200,
                StatusMessage = "Logged out successfully"
            });
        }

        [HttpGet]
        [AdminAndAbove]
        [Route("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _userService.GetAllUsers();
                return Ok(new ResponseDto<List<User>>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = "Users retrieved successfully.",
                    Data = users
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<List<User>>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "An error occurred while retrieving users."
                });
            }
        }

        [HttpGet]
        [AdminAndAbove]
        [Route("users/{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            try
            {
                var user = await _userService.GetUserById(id);
                if (user == null)
                {
                    return NotFound(new ResponseDto<User>
                    {
                        IsSuccess = false,
                        StatusCode = 404,
                        StatusMessage = "User not found."
                    });
                }

                return Ok(new ResponseDto<User>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = "User retrieved successfully.",
                    Data = user
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<User>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "An error occurred while retrieving the user."
                });
            }
        }

        [HttpGet]
        [Route("me")]
        [AllRoles]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ResponseDto<object>
                {
                    IsSuccess = false,
                    StatusCode = 401,
                    StatusMessage = "Invalid token"
                });
            }

            var currentUser = await _userService.GetUserById(int.Parse(userId));

            if (currentUser == null)
            {
                return NotFound(new ResponseDto<object>
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    StatusMessage = "User not found"
                });
            }

            return Ok(new ResponseDto<object>
            {
                IsSuccess = true,
                StatusCode = 200,
                StatusMessage = "User retrieved successfully",
                Data = currentUser
            });
        }

        [HttpPut]
        [Route("me")]
        [AllRoles]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest dto)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return Unauthorized(new ResponseDto<bool> { IsSuccess = false, StatusCode = 401, StatusMessage = "Unauthorized" });

            var result = await _authService.UpdateProfileAsync(userId, dto);
            if (result)
                return Ok(new ResponseDto<bool> { IsSuccess = true, StatusCode = 200, StatusMessage = "Profile updated successfully." });

            return BadRequest(new ResponseDto<bool> { IsSuccess = false, StatusCode = 400, StatusMessage = "Failed to update profile. Username may already be taken." });
        }

        [HttpPost]
        [Route("me/change-password")]
        [AllRoles]
        public async Task<IActionResult> ChangeMyPassword([FromBody] ChangePassword dto)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return Unauthorized(new ResponseDto<bool> { IsSuccess = false, StatusCode = 401, StatusMessage = "Unauthorized" });

            if (dto.NewPassword != dto.ConfirmNewPassword)
                return BadRequest(new ResponseDto<bool> { IsSuccess = false, StatusCode = 400, StatusMessage = "New password and confirmation do not match." });

            var result = await _authService.ChangePasswordAsync(userId, dto);
            if (result)
                return Ok(new ResponseDto<bool> { IsSuccess = true, StatusCode = 200, StatusMessage = "Password changed successfully." });

            return BadRequest(new ResponseDto<bool> { IsSuccess = false, StatusCode = 400, StatusMessage = "Failed to change password. Current password may be incorrect." });
        }

        [HttpPost]
        [AdminAndAbove]
        [Route("users/{userId}/lock")]
        public async Task<IActionResult> LockUserAccount(int userId, [FromBody] LockUser requestDto = null)
        {
            try
            {
                DateTime? lockoutEnd = requestDto?.LockoutEnd;
                var result = await _userService.LockUserAccountAsync(userId, lockoutEnd);

                if (result)
                {
                    return Ok(new ResponseDto<bool>
                    {
                        IsSuccess = true,
                        StatusCode = 200,
                        StatusMessage = "User account locked successfully."
                    });
                }

                return BadRequest(new ResponseDto<bool>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = "Failed to lock user account."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<bool>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "An error occurred while locking the user account."
                });
            }
        }

        [HttpPost]
        [AdminAndAbove]
        [Route("users/{userId}/unlock")]
        public async Task<IActionResult> UnlockUserAccount(int userId)
        {
            try
            {
                var result = await _userService.UnlockUserAccountAsync(userId);

                if (result)
                {
                    return Ok(new ResponseDto<bool>
                    {
                        IsSuccess = true,
                        StatusCode = 200,
                        StatusMessage = "User account unlocked successfully."
                    });
                }

                return BadRequest(new ResponseDto<bool>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = "Failed to unlock user account."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<bool>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "An error occurred while unlocking the user account."
                });
            }
        }

        [HttpPost]
        [AdminAndAbove]
        [Route("users/{userId}/reset-password")]
        public async Task<IActionResult> ResetUserPassword(int userId, [FromBody] ResetPasswordRequest requestDto)
        {
            if (requestDto == null || string.IsNullOrWhiteSpace(requestDto.NewPassword))
            {
                return BadRequest(new ResponseDto<bool>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = "New password is required."
                });
            }

            try
            {
                var result = await _userService.ResetUserPasswordAsync(userId, requestDto.NewPassword);

                if (result)
                {
                    return Ok(new ResponseDto<bool>
                    {
                        IsSuccess = true,
                        StatusCode = 200,
                        StatusMessage = "User password reset successfully."
                    });
                }

                return BadRequest(new ResponseDto<bool>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = "Failed to reset user password."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<bool>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "An error occurred while resetting the user password."
                });
            }
        }

        [HttpPost]
        [AdminAndAbove]
        [Route("users/{userId}/verify-email")]
        public async Task<IActionResult> VerifyUserEmail(int userId)
        {
            try
            {
                var result = await _userService.VerifyUserEmailAsync(userId);

                if (result)
                {
                    return Ok(new ResponseDto<bool>
                    {
                        IsSuccess = true,
                        StatusCode = 200,
                        StatusMessage = "User email verified successfully."
                    });
                }

                return BadRequest(new ResponseDto<bool>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = "Failed to verify user email."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<bool>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "An error occurred while verifying the user email."
                });
            }
        }

        [HttpPut]
        [AdminAndAbove]
        [Route("users/{userId}")]
        public async Task<IActionResult> UpdateUser(int userId, [FromBody] UpdateUserRequest updateUserDto)
        {
            if (updateUserDto == null || !ModelState.IsValid)
            {
                return BadRequest(new ResponseDto<bool>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = "Invalid request data.",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            try
            {
                var result = await _userService.UpdateUserAsync(userId, updateUserDto);

                if (result)
                {
                    return Ok(new ResponseDto<bool>
                    {
                        IsSuccess = true,
                        StatusCode = 200,
                        StatusMessage = "User updated successfully."
                    });
                }

                return BadRequest(new ResponseDto<bool>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = "Failed to update user."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<bool>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "An error occurred while updating the user."
                });
            }
        }

        [HttpPost]
        [AdminAndAbove]
        [Route("users")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUser createUserDto)
        {
            if (createUserDto == null || !ModelState.IsValid)
            {
                return BadRequest(new ResponseDto<User>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = "Invalid request data.",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            try
            {
                var user = await _userService.CreateUserAsync(createUserDto);

                if (user != null)
                {
                    return StatusCode(201, new ResponseDto<User>
                    {
                        IsSuccess = true,
                        StatusCode = 201,
                        StatusMessage = "User created successfully.",
                        Data = user
                    });
                }

                return BadRequest(new ResponseDto<User>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = "Failed to create user."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<User>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "An error occurred while creating the user."
                });
            }
        }

        [HttpDelete]
        [AdminAndAbove]
        [Route("users/{userId}")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            try
            {
                var result = await _userService.DeleteUserAsync(userId);

                if (result)
                {
                    return Ok(new ResponseDto<bool>
                    {
                        IsSuccess = true,
                        StatusCode = 200,
                        StatusMessage = "User deleted successfully."
                    });
                }

                return BadRequest(new ResponseDto<bool>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = "Failed to delete user."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<bool>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "An error occurred while deleting the user."
                });
            }
        }
    }
}
