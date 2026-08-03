using TicketSystem.Dtos.Product;
using TicketSystem.Models;

namespace TicketSystem.Interfaces;

public interface IProductRepository
{
    Task<Product> CreateAsync(Product productModel, CancellationToken cancellationToken = default);
    Task<Product?> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Product?> UpdateAsync(int id, UpdateProductRequestDto productDto, CancellationToken cancellationToken = default);
    Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> HasTicketHistoryAsync(int id, CancellationToken cancellationToken = default);
}
