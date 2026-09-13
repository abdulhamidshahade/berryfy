using Berryfy.Domain.Entities.AuthEntities;
using Microsoft.AspNetCore.Identity;

namespace Berryfy.Application.Services.Concretes.AuthServiceConcretes;

internal static class PasswordSessionRevocation
{
    public static async Task<IdentityResult> ExecuteAsync(ApplicationUser user, Func<Task<IdentityResult>> changePassword)
    {
        var token = user.RefreshToken;
        var expiry = user.RefreshTokenExpiry;
        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        var succeeded = false;
        try
        {
            var result = await changePassword();
            succeeded = result.Succeeded;
            return result;
        }
        finally
        {
            if (!succeeded)
            {
                user.RefreshToken = token;
                user.RefreshTokenExpiry = expiry;
            }
        }
    }
}
