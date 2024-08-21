using TicketSystem.Controllers;
using TicketSystem.Data;
using TicketSystem.Dtos.Account;
using TicketSystem.Helpers;
using TicketSystem.Interfaces;
using TicketSystem.Models;
using TicketSystem.Models.AppUserModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace TicketSystem.Repository
{
     public class AccountRepository : IAccountRepository
     {
          private readonly ApplicationDbContext _context;
          private readonly UserManager<AppUser> _userManager;
          private readonly ILogger<AccountController> _logger;

          public AccountRepository(ApplicationDbContext context, UserManager<AppUser> userManager, ILogger<AccountController> logger)
          {
               _context = context;
               _userManager = userManager;
               _logger = logger;
          }

          public async Task<FirmUser> CreateAsyncs(FirmUser firmUser)
          {
               await _context.FirmUsers.AddAsync(firmUser);
               await _context.SaveChangesAsync();
               return firmUser;
          }

          public async Task<AppUser?> DeleteAsync(string id)
          {
               try
               {
                    var accountModel = await _userManager.FindByIdAsync(id);

                    if (accountModel == null)
                    {
                         return null;
                    }

                    _logger.LogInformation("Removing related FirmUsers for user {UserId}", id);

                    await _userManager.DeleteAsync(accountModel);
                    await _context.SaveChangesAsync();

                    return accountModel;
               }
               catch (Exception ex)
               {
                    // Log the exception
                    _logger.LogError(ex, "Error deleting user with ID {UserId}", id);
                    throw;
               }
          }

          public async Task<List<AppUserSummary>> GetAllAsync()
          {
               var users = await _context.Users
                    .Include(u => u.FirmUsers)
                    .ThenInclude(fu => fu.Firm)
                   .Select(t => new AppUserSummary
                   {
                        Id = t.Id,
                        UserName = t.UserName,
                        LastName = t.LastName,
                        Email = t.Email,
                        Role = t.Role,
                        FirmName = t.FirmUsers
                          .Select(cu => cu.Firm.Name)
                          .FirstOrDefault()
                   }).ToListAsync();

               return users;
          }

          public async Task<AppUser?> GetByIdAsync(Guid id)
          {
               string stringId = id.ToString();
               return await _context.Users.FindAsync(stringId);
          }

          public async Task<AppUser?> GetByNameAsync(string name)
          {
               return await _context.Users.FirstOrDefaultAsync(t => t.UserName == name);
          }

          public async Task<AppUser?> UpdateAsync(string id, UpdateDto appuserDto)
          {
               var existingUser = await _context.Users.FindAsync(id);
               if (existingUser == null)
               {
                    return null;
               }
               existingUser.Id = appuserDto.Id;
               existingUser.UserName = appuserDto.Name;
               existingUser.LastName = appuserDto.LastName;
               existingUser.Email = appuserDto.Email;
               existingUser.Role = appuserDto.Role;

               await _context.SaveChangesAsync();
               return existingUser;
          }
     }
}
