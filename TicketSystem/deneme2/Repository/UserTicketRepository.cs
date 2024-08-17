using TicketSystem.Data;
using TicketSystem.Interfaces;
using TicketSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace TicketSystem.Repository
{
     public class UserTicketRepository : IUserTicketRepository
     {
          private readonly ApplicationDbContext _context;
          public UserTicketRepository(ApplicationDbContext context)
          {
               _context = context;
          }

          public async Task<AppUserTicket> CreateAsync(AppUserTicket appuserticket)
          {
               await _context.AppUserTickets.AddAsync(appuserticket);
               await _context.SaveChangesAsync();
               return appuserticket;
          }

          public async Task<AppUserTicket> DeleteAppUserTicket(AppUser appUser, string title)
          {
               var appuserticketModel = await _context.AppUserTickets.FirstOrDefaultAsync(x => x.AppUserId == appUser.Id && x.Ticket.Title.ToLower() == title.ToLower());

               if (appuserticketModel == null)
               {
                    return null;
               }

               _context.AppUserTickets.Remove(appuserticketModel);
               await _context.SaveChangesAsync();
               return appuserticketModel;
          }

          public async Task<List<Ticket>> GetUserAppUserTicket(AppUser user)
          {
               return await _context.AppUserTickets
                   .Select(ticket => new Ticket
                   {
                        Id = ticket.TicketId,
                        Title = ticket.Ticket.Title,
                        Description = ticket.Ticket.Description,
                   }).ToListAsync();
          }
     }
}