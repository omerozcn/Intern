using TicketSystem.Dtos.Common;
using TicketSystem.Dtos.Firm;
using TicketSystem.Models;

namespace TicketSystem.Interfaces;

public interface IFirmRepository
{
    Task<Firm> CreateAsync(Firm firmModel, CancellationToken cancellationToken = default);
    Task<Firm?> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResult<FirmDto>> GetAllAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<Firm?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Firm?> UpdateAsync(int id, UpdateFirmRequestDto firmModel, CancellationToken cancellationToken = default);
    Task<Firm?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<bool> HasTicketHistoryAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> HasUsersAsync(int id, CancellationToken cancellationToken = default);
}
