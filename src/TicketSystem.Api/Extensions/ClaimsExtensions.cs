using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TicketSystem.Security;

namespace TicketSystem.Extensions;

public static class ClaimsExtensions
{
    public static string GetUserName(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(AuthClaimTypes.UserName)
            ?? principal.FindFirstValue(ClaimTypes.GivenName)
            ?? principal.Identity?.Name
            ?? string.Empty;
    }

    public static string? GetUserId(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(AuthClaimTypes.UserId)
            ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    public static int? GetFirmId(this ClaimsPrincipal principal)
    {
        return int.TryParse(
            principal.FindFirstValue(AuthClaimTypes.FirmId),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var firmId) && firmId > 0
            ? firmId
            : null;
    }

    public static string? GetFirmName(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(AuthClaimTypes.FirmName);
    }

    /// <summary>
    /// An administrator belonging to the firm that owns the platform.
    ///
    /// Both halves matter: the firm claim alone would promote a plain user who somehow
    /// ended up in that firm, and the role alone is what every other administrator has.
    /// </summary>
    public static bool IsSuperAdmin(this ClaimsPrincipal principal)
    {
        return principal.IsInRole(AppRoles.Admin)
            && ProtectedFirm.IsProtectedName(principal.GetFirmName());
    }

    public static string GetDisplayName(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(AuthClaimTypes.Name)
            ?? principal.FindFirstValue(AuthClaimTypes.UserName)
            ?? principal.Identity?.Name
            ?? string.Empty;
    }
}
