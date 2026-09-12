using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Berryfy.Domain.Constants;

namespace Berryfy.API.Controllers
{
    [ApiController]
    [Route("api/base")]
    public class BaseController : ControllerBase
    {
        protected BaseController()
        {
        }

        protected int? GetCurrentUserId()
        {
            var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null && int.TryParse(userIdClaim.Value, out int id) ? id : null;
        }

        protected bool CanAccessUserResource(int? ownerId)
        {
            return GetCurrentUserId().HasValue &&
                (ownerId == GetCurrentUserId() ||
                 User.IsInRole(RoleConstants.Admin) ||
                 User.IsInRole(RoleConstants.SuperAdmin));
        }

        protected string? GetSessionId()
        {
            string? sessionId = Request.Headers["X-Session-Id"];

            if (string.IsNullOrEmpty(sessionId) && !GetCurrentUserId().HasValue)
            {
                

                return null;
            }

            return sessionId;
        }
    }
}
