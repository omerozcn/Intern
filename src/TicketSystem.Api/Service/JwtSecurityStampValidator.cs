using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using TicketSystem.Interfaces;
using TicketSystem.Models;
using TicketSystem.Security;

namespace TicketSystem.Service;

public sealed class JwtSecurityStampValidator : IJwtSecurityStampValidator
{
    private readonly UserManager<AppUser> _userManager;

    public JwtSecurityStampValidator(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<bool> ValidateAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var userId = principal.FindFirstValue(AuthClaimTypes.UserId)
            ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var stampFingerprint = principal.FindFirstValue(AuthClaimTypes.SecurityStamp);

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(stampFingerprint))
        {
            return false;
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null || await _userManager.IsLockedOutAsync(user))
        {
            return false;
        }

        var currentStamp = await _userManager.GetSecurityStampAsync(user);
        return !string.IsNullOrWhiteSpace(currentStamp)
            && SecurityStampFingerprint.Matches(stampFingerprint, currentStamp);
    }
}
