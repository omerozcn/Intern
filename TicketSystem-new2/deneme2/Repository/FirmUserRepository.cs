using TicketSystem.Data;
using TicketSystem.Interfaces;
using TicketSystem.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace TicketSystem.Repository
{
     public class FirmUserRepository : IFirmUserRepository
     {
          private readonly ApplicationDbContext _context;

          public FirmUserRepository(ApplicationDbContext context)
          {
               _context = context;
          }

          public async Task<List<Firm>> GetUserFirmUser(AppUser appUser)
          {
               return await _context.FirmUsers
                   .Select(firm => new Firm
                   {
                        Id = firm.Id,
                        Name = firm.Firm.Name,
                   }).ToListAsync();
          }

          public async Task<FirmUser> GetByAppUserIdAsync(string appUserId)
          {
               return await _context.FirmUsers
                   .Include(fu => fu.Firm)
                   .FirstOrDefaultAsync(fu => fu.AppUserId == appUserId);
          }

          public async Task<FirmUser> AddAsync(FirmUser firmUser)
          {
               await _context.FirmUsers.AddAsync(firmUser);
               await _context.SaveChangesAsync();
               return firmUser;
          }
     }
}
