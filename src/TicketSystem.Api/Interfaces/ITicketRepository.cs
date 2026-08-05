using TicketSystem.Dtos.Common;
using TicketSystem.Dtos.Ticket;
using TicketSystem.Models;

namespace TicketSystem.Interfaces;

public interface ITicketRepository
{
    Task<Ticket?> CreateAsync(
        Ticket ticketModel,
        string appUserId,
        int firmId,
        int? productId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<TicketDto>> GetAllAsync(TicketListRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<TicketDto>> GetByUserIdAsync(string appUserId, TicketListRequest request, CancellationToken cancellationToken = default);
    Task<TicketDto?> GetDtoByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Ticket?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> IsOwnedByAsync(int ticketId, string appUserId, CancellationToken cancellationToken = default);
    Task<Ticket?> UpdateAsync(int id, string? answer, int status, CancellationToken cancellationToken = default);
    Task<Ticket?> UpdateDescriptionAsync(int id, string appUserId, string description, CancellationToken cancellationToken = default);
    Task<Ticket?> UpdateTicketStatusAsync(int id, int status, CancellationToken cancellationToken = default);
    Task<Ticket?> DeleteAsync(int id, string appUserId, CancellationToken cancellationToken = default);
    Task<int?> GetFirmIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TicketStatusCountDto>> GetStatusCountsAsync(string? appUserId, int? firmId = null, CancellationToken cancellationToken = default);
}
