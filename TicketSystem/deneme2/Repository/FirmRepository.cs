using TicketSystem.Data;
using TicketSystem.Dtos.Firm;
using TicketSystem.Interfaces;
using TicketSystem.Models;
using TicketSystem.Models.FirmModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace TicketSystem.Repository
{
     public class FirmRepository : IFirmRepository
     {
          private readonly ApplicationDbContext _context;
          private readonly UserManager<AppUser> _userManager;
          public FirmRepository(ApplicationDbContext context, UserManager<AppUser> userManager)
          {
               _context = context;
               _userManager = userManager;
          }

          public async Task<Firm> CreateAsync(Firm firmModel)
          {
               await _context.Firms.AddAsync(firmModel);
               await _context.SaveChangesAsync();
               return firmModel;
          }

          public async Task<Firm?> DeleteAsync(string id)
          {
               var firmid = Convert.ToInt32(id);
               var firmModel = await _context.Firms.FirstOrDefaultAsync(x => x.Id == firmid);

               if (firmModel == null)
               {
                    return null;
               }
               _context.Firms.Remove(firmModel);

               await _context.SaveChangesAsync();
               return firmModel;
          }

          public async Task<List<FirmSummary>> GetAllAsync()
          {
               var firms = await _context.Firms
                   .Select(t => new FirmSummary
                   {
                        Id = t.Id,
                        Name = t.Name
                   }).ToListAsync();

               return firms;
          }

          public async Task<Firm?> GetByIdAsync(int id)
          {
               return await _context.Firms.FindAsync(id);
          }

          public async Task<Firm?> GetByNameAsync(string name)
          {
               return await _context.Firms.FirstOrDefaultAsync(t => t.Name == name);
          }

          public async Task<Firm?> UpdateAsync(int id, UpdateFirmRequestDto firmDto)
          {
               var existingFirm = await _context.Firms.FirstOrDefaultAsync(x => x.Id == id);
               if (existingFirm == null)
               {
                    return null;
               }

               existingFirm.Name = firmDto.Name;

               await _context.SaveChangesAsync();
               return existingFirm;
          }
     }
}
