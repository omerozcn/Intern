using TicketSystem.Models;

namespace TicketSystem.Interfaces
{
     public interface ITokenService
     {
          Task<string> CreateTokenAsync(AppUser user);

          Task StoreTokenAsync(AppUser user, string token);
     }
}

