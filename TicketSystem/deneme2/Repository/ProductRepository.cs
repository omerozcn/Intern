using System.Data;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Data;
using TicketSystem.Dtos.Common;
using TicketSystem.Dtos.Product;
using TicketSystem.Extensions;
using TicketSystem.Interfaces;
using TicketSystem.Models;

namespace TicketSystem.Repository;

public sealed class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _context;

    public ProductRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Product> CreateAsync(
        Product productModel,
        CancellationToken cancellationToken = default)
    {
        await _context.Products.AddAsync(productModel, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return productModel;
    }

    public async Task<Product?> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var productModel = await _context.Products
            .FirstOrDefaultAsync(product => product.Id == id, cancellationToken);

        if (productModel == null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        if (await _context.ProductTickets.AnyAsync(link => link.ProductId == id, cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        _context.Products.Remove(productModel);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return productModel;
    }

    public async Task<PagedResult<ProductDto>> GetAllAsync(
        PageRequest request,
        CancellationToken cancellationToken = default)
    {
        var products = _context.Products.AsNoTracking();

        if (request.Search is not null)
        {
            products = products.Where(product => product.Name.Contains(request.Search));
        }

        // Assigned firms travel with the product so clients do not join the two lists themselves.
        return await products
            .OrderBy(product => product.Name)
            .Select(product => new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                BirthDate = product.BirthDate,
                Firms = product.FirmProducts
                    .OrderBy(link => link.Firm.Name)
                    .Select(link => new ProductFirmDto
                    {
                        RelationId = link.Id,
                        Id = link.FirmId,
                        Name = link.Firm.Name,
                    })
                    .ToList(),
            })
            .ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .FirstOrDefaultAsync(product => product.Id == id, cancellationToken);
    }

    public async Task<Product?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(product => product.Name == name, cancellationToken);
    }

    public async Task<bool> HasTicketHistoryAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.ProductTickets
            .AsNoTracking()
            .AnyAsync(productTicket => productTicket.ProductId == id, cancellationToken);
    }

    public async Task<Product?> UpdateAsync(
        int id,
        UpdateProductRequestDto productDto,
        CancellationToken cancellationToken = default)
    {
        var existingProduct = await _context.Products
            .FirstOrDefaultAsync(product => product.Id == id, cancellationToken);
        if (existingProduct == null)
        {
            return null;
        }

        existingProduct.Name = productDto.Name ?? existingProduct.Name;
        await _context.SaveChangesAsync(cancellationToken);
        return existingProduct;
    }
}
