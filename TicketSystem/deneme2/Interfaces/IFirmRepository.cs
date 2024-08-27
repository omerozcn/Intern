using TicketSystem.Models;
using TicketSystem.Models.FirmModels;
using TicketSystem.Dtos.Firm;

namespace TicketSystem.Interfaces
{
     public interface IFirmRepository
     {
          Task<Firm> CreateAsync(Firm firmModel);
          Task<Firm?> DeleteAsync(string id);
          Task<List<FirmSummary>> GetAllAsync();
          Task<Firm?> GetByIdAsync(int id);
          Task<Firm?> UpdateAsync(int id, UpdateFirmRequestDto firmModel);
          Task<Firm?> GetByNameAsync(string name);
     }
}
