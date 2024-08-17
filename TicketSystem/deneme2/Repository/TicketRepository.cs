using TicketSystem.Data;
using TicketSystem.Dtos.Ticket;
using TicketSystem.Helpers;
using TicketSystem.Interfaces;
using TicketSystem.Models;
using TicketSystem.Models.TicketModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net.Sockets;

namespace TicketSystem.Repository
{
     public class TicketRepository : ITicketRepository
     {
          private readonly ApplicationDbContext _context;
          private readonly UserManager<AppUser> _userManager;
          private readonly ILogger<TicketRepository> _logger;
          public TicketRepository(ApplicationDbContext context, UserManager<AppUser> userManager, ILogger<TicketRepository> logger)
          {
               _context = context;
               _userManager = userManager;
               _logger = logger;
          }

          public async Task<Ticket> CreateAsync(Ticket ticketModel)
          {
               await _context.Tickets.AddAsync(ticketModel);
               await _context.SaveChangesAsync();
               return ticketModel;
          }

          public async Task<AppUserTicket> CreateAsyncs(AppUserTicket appuserticket)
          {

               await _context.AppUserTickets.AddAsync(appuserticket);
               await _context.SaveChangesAsync();
               return appuserticket;
          }

          public async Task<Ticket?> DeleteAsync(int id)
          {
               var ticketModel = await _context.Tickets.FirstOrDefaultAsync(x => x.Id == id);

               if (ticketModel == null)
               {
                    return null;
               }
               _context.Tickets.Remove(ticketModel);

               await _context.SaveChangesAsync();
               return ticketModel;
          }

          public async Task<List<TicketSummary>> GetAllAsync()
          {
               var tickets = await _context.Tickets
                   .Select(t => new TicketSummary
                   {
                        Id = t.Id,
                        Title = t.Title,
                        Created = t.Created,
                        CreatedBy = t.CreatedBy,
                   }).ToListAsync();

               return tickets;
          }

          public async Task<Ticket?> GetByIdAsync(int id)
          {
               return await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id);
          }

          public async Task<Ticket?> GetByNameAsync(string name)
          {
               return await _context.Tickets.FirstOrDefaultAsync(t => t.Title == name);
          }

          public async Task<Ticket?> UpdateAsync(int id, UpdateTicketRequestDto ticketDto)
          {
               var existingTicket = await _context.Tickets.FirstOrDefaultAsync(x => x.Id == id);
               if (existingTicket == null)
               {
                    return null;
               }

               existingTicket.Title = ticketDto.Title;
               existingTicket.Description = ticketDto.Description;
               existingTicket.Updated = DateTime.Now;

               await _context.SaveChangesAsync();
               return existingTicket;
          }
     }
}
