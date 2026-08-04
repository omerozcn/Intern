using TicketSystem.Dtos.Account;
using TicketSystem.Dtos.Token;
using TicketSystem.Models;

namespace TicketSystem.Interfaces;

public interface ITokenService
{
    Task<IssuedTokenDto> CreateTokenAsync(
        AppUser user,
        ProfileDto profile,
        CancellationToken cancellationToken = default);
}
