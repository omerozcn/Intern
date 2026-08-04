using System.Security.Claims;

namespace TicketSystem.Interfaces;

public interface IJwtSecurityStampValidator
{
    Task<bool> ValidateAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);
}
