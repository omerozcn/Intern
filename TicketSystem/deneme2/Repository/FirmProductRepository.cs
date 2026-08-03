using Microsoft.EntityFrameworkCore;
using TicketSystem.Data;
using TicketSystem.Dtos.FirmProduct;
using TicketSystem.Dtos.Product;
using TicketSystem.Interfaces;
using TicketSystem.Models;

namespace TicketSystem.Repository;

public sealed class FirmProductRepository : IFirmProductRepository
{
    private readonly ApplicationDbContext _context;

    public FirmProductRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FirmProductCreateResult> CreateAsync(
        CreateFirmProductRequestDto firmProductDto,
        CancellationToken cancellationToken = default)
    {
        var firmExists = await _context.Firms
            .AnyAsync(firm => firm.Id == firmProductDto.FirmId, cancellationToken);
        var productExists = await _context.Products
            .AnyAsync(product => product.Id == firmProductDto.ProductId, cancellationToken);

        if (!firmExists || !productExists)
        {
            return FirmProductCreateResult.UnknownFirmOrProduct();
        }

        var alreadyAssigned = await _context.FirmProducts.AnyAsync(
            link => link.FirmId == firmProductDto.FirmId && link.ProductId == firmProductDto.ProductId,
            cancellationToken);
        if (alreadyAssigned)
        {
            return FirmProductCreateResult.Duplicate();
        }

        var firmProduct = new FirmProduct
        {
            FirmId = firmProductDto.FirmId,
            ProductId = firmProductDto.ProductId,
        };

        await _context.FirmProducts.AddAsync(firmProduct, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return FirmProductCreateResult.Created(new FirmProductDto
        {
            Id = firmProduct.Id,
            FirmId = firmProduct.FirmId,
            ProductId = firmProduct.ProductId,
        });
    }

    public async Task<FirmProduct?> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var firmProduct = await _context.FirmProducts
            .FirstOrDefaultAsync(link => link.Id == id, cancellationToken);
        if (firmProduct == null)
        {
            return null;
        }

        _context.FirmProducts.Remove(firmProduct);
        await _context.SaveChangesAsync(cancellationToken);
        return firmProduct;
    }

    public async Task<IReadOnlyList<FirmProductDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await AssignmentRows()
            .OrderBy(link => link.FirmName)
            .ThenBy(link => link.ProductName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FirmProductDto>> GetFirmProductAsync(
        string firmName,
        CancellationToken cancellationToken = default)
    {
        return await AssignmentRows()
            .Where(link => link.FirmName == firmName)
            .OrderBy(link => link.ProductName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CurrentUserProductDto>> GetProductsByFirmIdAsync(
        int firmId,
        CancellationToken cancellationToken = default)
    {
        return await _context.FirmProducts
            .AsNoTracking()
            .Where(link => link.FirmId == firmId)
            .OrderBy(link => link.Products.Name)
            .Select(link => new CurrentUserProductDto
            {
                Id = link.ProductId,
                Name = link.Products.Name,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<int?> GetFirmIdForUserAsync(
        string appUserId,
        CancellationToken cancellationToken = default)
    {
        return await _context.FirmUsers
            .AsNoTracking()
            .Where(firmUser => firmUser.AppUserId == appUserId)
            .Select(firmUser => (int?)firmUser.FirmId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private IQueryable<FirmProductDto> AssignmentRows()
    {
        return _context.FirmProducts
            .AsNoTracking()
            .Select(link => new FirmProductDto
            {
                Id = link.Id,
                FirmId = link.FirmId,
                FirmName = link.Firm.Name,
                ProductId = link.ProductId,
                ProductName = link.Products.Name,
            });
    }
}
