using TicketSystem.Dtos.Ticket;
using TicketSystem.Models;
using TicketSystem.Models.TicketModels;

namespace TicketSystem.Interfaces
{
     public interface ITicketRepository
     {
          Task<Ticket> CreateAsync(Ticket ticketModel);
          Task<AppUserTicket> AppUserTicketCreateAsync(AppUserTicket appuserticket);
          Task<ProductTicket> ProductTicketCreateAsync(ProductTicket productTicket);
          Task<List<TicketDto>> GetByUserIdAsync(string id);
          Task<Ticket?> DeleteAsync(int id);
          Task<List<TicketDto>> GetAllAsync();
          Task<Ticket?> GetByIdAsync(int id);
          Task<Ticket?> UpdateAsync(int id, UpdateTicketRequestDto ticketDto);
          Task<Ticket?> UpdateTicketStatusAsync(Ticket ticket);
     }
}
