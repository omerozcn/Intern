using System.Data;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Data;
using TicketSystem.Dtos.Firm;
using TicketSystem.Interfaces;
using TicketSystem.Models;

namespace TicketSystem.Repository;

public sealed class FirmRepository : IFirmRepository
{
    private readonly ApplicationDbContext _context;

    public FirmRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Firm> CreateAsync(Firm firmModel, CancellationToken cancellationToken = default)
    {
        await _context.Firms.AddAsync(firmModel, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return firmModel;
    }

    public async Task<Firm?> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var firmModel = await _context.Firms
            .FirstOrDefaultAsync(firm => firm.Id == id, cancellationToken);

        if (firmModel == null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        if (await _context.FirmUsers.AnyAsync(firmUser => firmUser.FirmId == id, cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        _context.Firms.Remove(firmModel);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return firmModel;
    }

    public async Task<bool> HasTicketHistoryAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.FirmUsers
            .AsNoTracking()
            .Where(firmUser => firmUser.FirmId == id)
            .AnyAsync(firmUser => firmUser.AppUser.AppUserTickets.Any(), cancellationToken);
    }

    public async Task<bool> HasUsersAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.FirmUsers
            .AsNoTracking()
            .AnyAsync(firmUser => firmUser.FirmId == id, cancellationToken);
    }

    public async Task<IReadOnlyList<FirmDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Firms
            .AsNoTracking()
            .OrderBy(firm => firm.Name)
            .Select(firm => new FirmDto
            {
                Id = firm.Id,
                Name = firm.Name,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<Firm?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Firms
            .FirstOrDefaultAsync(firm => firm.Id == id, cancellationToken);
    }

    public async Task<Firm?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Firms
            .FirstOrDefaultAsync(firm => firm.Name == name, cancellationToken);
    }

    public async Task<Firm?> UpdateAsync(
        int id,
        UpdateFirmRequestDto firmDto,
        CancellationToken cancellationToken = default)
    {
        var existingFirm = await _context.Firms
            .FirstOrDefaultAsync(firm => firm.Id == id, cancellationToken);
        if (existingFirm == null)
        {
            return null;
        }

        existingFirm.Name = firmDto.Name ?? existingFirm.Name;
        await _context.SaveChangesAsync(cancellationToken);
        return existingFirm;
    }
}
