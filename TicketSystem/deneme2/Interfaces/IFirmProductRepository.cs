using TicketSystem.Dtos.FirmProduct;
using TicketSystem.Dtos.Product;
using TicketSystem.Models;

namespace TicketSystem.Interfaces;

public interface IFirmProductRepository
{
    Task<FirmProductCreateResult> CreateAsync(CreateFirmProductRequestDto firmProductDto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FirmProductDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<FirmProduct?> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FirmProductDto>> GetFirmProductAsync(string firmName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CurrentUserProductDto>> GetProductsByFirmIdAsync(int firmId, CancellationToken cancellationToken = default);
    Task<int?> GetFirmIdForUserAsync(string appUserId, CancellationToken cancellationToken = default);
}
