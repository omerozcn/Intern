using TicketSystem.Dtos.Ticket;
using TicketSystem.Models;
using TicketSystem.Models.TicketModels;

namespace TicketSystem.Interfaces
{
     public interface ITicketRepository
     {
          Task<Ticket> CreateAsync(Ticket ticketModel);
          Task<AppUserTicket> CreateAsyncs(AppUserTicket appuserticket);
          Task<Ticket?> DeleteAsync(int id);
          Task<List<TicketDto>> GetAllAsync();
          Task<Ticket?> GetByIdAsync(int id);
          Task<Ticket?> UpdateAsync(int id, UpdateTicketRequestDto ticketDto);
          Task<Ticket?> GetByNameAsync(string name);
          Task<Ticket?> UpdateTicketStatusAsync(Ticket ticket);
     }
}
