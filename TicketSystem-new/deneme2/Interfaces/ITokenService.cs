using TicketSystem.Models;

namespace TicketSystem.Interfaces
{
     public interface ITokenService
     {
          Task<string> CreateTokenAsync(AppUser user);
     }
}

