using TicketSystem.Dtos.Account;
using TicketSystem.Models;
using TicketSystem.Models.AppUserModels;

namespace TicketSystem.Interfaces
{
     public interface IAccountRepository
     {
          Task<AppUser?> DeleteAsync(string id);
          Task<List<AppUserSummary>> GetAllAsync();
          Task<FirmUser> CreateAsyncs(FirmUser firmUser);
          Task<AppUser?> GetByIdAsync(Guid id);
          Task<AppUser?> UpdateAsync(string id, UpdateDto appuserDto);
          Task<AppUser?> GetByNameAsync(string name);
          Task<AppUser?> GetByUserNameAsync(string name);
          Task<string> GenerateUniqueUserNameAsync(int length);
          Task<string?> GetUserRoleAsync(string role);
     }
}
