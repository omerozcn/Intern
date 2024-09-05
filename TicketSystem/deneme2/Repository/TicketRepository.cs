using TicketSystem.Data;
using TicketSystem.Dtos.Ticket;
using TicketSystem.Interfaces;
using TicketSystem.Models;
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
               ticketModel.Status = 1;
               await _context.Tickets.AddAsync(ticketModel);
               await _context.SaveChangesAsync();
               return ticketModel;
          }

          public async Task<ProductTicket> ProductTicketCreateAsync(ProductTicket productTicket)
          {
               await _context.ProductTickets.AddAsync(productTicket);
               await _context.SaveChangesAsync();
               return productTicket;
          }

          public async Task<AppUserTicket> AppUserTicketCreateAsync(AppUserTicket appuserticket)
          {

               await _context.AppUserTickets.AddAsync(appuserticket);
               await _context.SaveChangesAsync();
               return appuserticket;
          }

          public async Task<Ticket?> DeleteAsync(int id)
          {
               var ticketModel = await _context.Tickets.FirstOrDefaultAsync(ticket => ticket.Id == id);

               if (ticketModel == null)
               {
                    return null;
               }
               _context.Tickets.Remove(ticketModel);

               await _context.SaveChangesAsync();
               return ticketModel;
          }

          public async Task<List<TicketDto>> GetAllAsync()
          {
               var tickets = await _context.Tickets
                    .GroupJoin(
                         _context.ProductTickets,
                         ticket => ticket.Id,
                         pt => pt.TicketId,
                         (ticket, pts) => new { ticket, pts })
                    .SelectMany(
                         ticket => ticket.pts.DefaultIfEmpty(),
                         (ticket, pt) => new { ticket.ticket, pt })
                    .GroupJoin(
                         _context.FirmProducts,
                         ticket => ticket.pt.ProductId,
                         fp => fp.ProductId,
                         (ticket, fps) => new { ticket.ticket, ticket.pt, fps })
                    .SelectMany(
                         ticket => ticket.fps.DefaultIfEmpty(),
                         (ticket, fp) => new { ticket.ticket, ticket.pt, fp })
                    .GroupJoin(
                         _context.Firms,
                         ticket => ticket.fp.FirmId,
                         f => f.Id,
                         (ticket, firms) => new { ticket.ticket, ticket.pt, ticket.fp, firms })
                    .SelectMany(
                         ticket => ticket.firms.DefaultIfEmpty(),
                         (ticket, firm) => new { ticket.ticket, ticket.pt, ticket.fp, firm })
                    .GroupJoin(
                         _context.Products,
                         ticket => ticket.pt.ProductId,
                         p => p.Id,
                         (ticket, products) => new { ticket.ticket, ticket.pt, ticket.fp, ticket.firm, products })
                    .SelectMany(
                         ticket => ticket.products.DefaultIfEmpty(),
                         (ticket, product) => new TicketDto
                         {
                              Id = ticket.ticket.Id,
                              NewProduct = ticket.ticket.NewProduct,
                              Description = ticket.ticket.Description,
                              Created = ticket.ticket.Created,
                              CreatedBy = ticket.ticket.CreatedBy,
                              Updated = new UpdateTicketRequestDto().Update,
                              Status = ticket.ticket.Status,
                              Answer = ticket.ticket.Answer,
                              FirmName = ticket.firm.Name,
                              ProductName = product.Name
                         })
                    .ToListAsync(); ;

               return tickets;
          }

          public async Task<Ticket?> GetByIdAsync(int id)
          {
               return await _context.Tickets.FirstOrDefaultAsync(ticket => ticket.Id == id);
          }

          public async Task<List<TicketDto>> GetByUserIdAsync(string id)
          {
               var ticketIds = await _context.AppUserTickets
                    .Where(ut => ut.AppUserId == id)
                    .Select(ut => ut.TicketId)
                    .ToListAsync();

               var tickets = await _context.Tickets
                    .Where(ticket => ticketIds.Contains(ticket.Id))
                    .Select(ticket => new TicketDto
                    {
                         Id = ticket.Id,
                         NewProduct = ticket.NewProduct,
                         Description = ticket.Description,
                         Created = ticket.Created,
                         CreatedBy = ticket.CreatedBy,
                         Updated = new UpdateTicketRequestDto().Update,
                         Status = ticket.Status,
                         Answer = ticket.Answer,
                         FirmName = (from fu in _context.FirmUsers
                                     join f in _context.Firms on fu.FirmId equals f.Id
                                     where fu.AppUserId == id
                                     select f.Name).FirstOrDefault(),
                         ProductName = (from pt in _context.ProductTickets
                                        join p in _context.Products on pt.ProductId equals p.Id
                                        where pt.TicketId == ticket.Id
                                        select p.Name).FirstOrDefault()
                    })
                    .ToListAsync();

               return tickets;
          }

          public async Task<Ticket?> UpdateAsync(int id, UpdateTicketRequestDto ticketDto)
          {
               var existingTicket = await _context.Tickets.FirstOrDefaultAsync(ticket => ticket.Id == id);
               if (existingTicket == null)
               {
                    return null;
               }
               existingTicket.Answer = ticketDto.Answer;
               existingTicket.Status = ticketDto.Status;
               existingTicket.Updated = DateTime.Now;

               await _context.SaveChangesAsync();
               return existingTicket;
          }
          public async Task<Ticket?> UpdateTicketStatusAsync(Ticket ticket)
          {
               ticket.Updated = DateTime.Now;
               _context.Tickets.Update(ticket);
               await _context.SaveChangesAsync();
               return ticket;
          }


     }
}
