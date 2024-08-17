using TicketSystem.Models;

namespace TicketSystem.Interfaces
{
     public interface IFirmUserRepository
     {
          Task<List<Firm>> GetUserFirmUser(AppUser appUser);
          Task<FirmUser> GetByAppUserIdAsync(string appUserId);
          Task<FirmUser> AddAsync(FirmUser firmUser);
     }
}
