using TicketSystem.Models;

namespace TicketSystem.Interfaces
{
     public interface IUserTicketRepository
     {
          Task<List<Ticket>> GetUserAppUserTicket(AppUser user);
          Task<AppUserTicket> CreateAsync(AppUserTicket appuserticket);
          Task<AppUserTicket> DeleteAppUserTicket(AppUser appUser, string title);
     }
}
