using System.Data;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Data;
using TicketSystem.Dtos.Ticket;
using TicketSystem.Interfaces;
using TicketSystem.Models;

namespace TicketSystem.Repository;

public sealed class TicketRepository : ITicketRepository
{
    private readonly ApplicationDbContext _context;

    public TicketRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Ticket?> CreateAsync(
        Ticket ticketModel,
        string appUserId,
        int firmId,
        int? productId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            if (!ticketModel.NewProduct)
            {
                if (!productId.HasValue || productId.Value <= 0)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return null;
                }

                var productBelongsToFirm = await _context.FirmProducts
                    .AsNoTracking()
                    .AnyAsync(
                        link => link.FirmId == firmId && link.ProductId == productId.Value,
                        cancellationToken);

                if (!productBelongsToFirm)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return null;
                }
            }

            ticketModel.Status = TicketStatuses.Pending;
            ticketModel.Created = DateTime.UtcNow;
            ticketModel.Updated = null;
            ticketModel.AppUserTickets.Add(new AppUserTicket
            {
                AppUserId = appUserId,
            });

            if (!ticketModel.NewProduct)
            {
                ticketModel.ProductTickets.Add(new ProductTicket
                {
                    ProductId = productId!.Value,
                });
            }

            await _context.Tickets.AddAsync(ticketModel, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return ticketModel;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<TicketDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await TicketRows()
            .OrderBy(row => row.Status)
            .ThenByDescending(row => row.Created)
            .ToListAsync(cancellationToken);

        return rows.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<TicketDto>> GetByUserIdAsync(
        string appUserId,
        CancellationToken cancellationToken = default)
    {
        var rows = await TicketRows(appUserId)
            .OrderBy(row => row.Status)
            .ThenByDescending(row => row.Created)
            .ToListAsync(cancellationToken);

        return rows.Select(ToDto).ToList();
    }

    public async Task<TicketDto?> GetDtoByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var row = await TicketRows().FirstOrDefaultAsync(row => row.Id == id, cancellationToken);
        return row == null ? null : ToDto(row);
    }

    public async Task<Ticket?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(ticket => ticket.Id == id, cancellationToken);
    }

    public async Task<bool> IsOwnedByAsync(
        int ticketId,
        string appUserId,
        CancellationToken cancellationToken = default)
    {
        return await _context.AppUserTickets
            .AsNoTracking()
            .AnyAsync(
                link => link.TicketId == ticketId && link.AppUserId == appUserId,
                cancellationToken);
    }

    public async Task<Ticket?> UpdateAsync(
        int id,
        string? answer,
        int status,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _context.Tickets
            .FirstOrDefaultAsync(ticket => ticket.Id == id, cancellationToken);
        if (ticket == null)
        {
            return null;
        }

        ticket.Answer = string.IsNullOrWhiteSpace(answer) ? null : answer.Trim();
        ticket.Status = status;
        ticket.Updated = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return ticket;
    }

    public async Task<Ticket?> UpdateDescriptionAsync(
        int id,
        string appUserId,
        string description,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var ticket = await _context.Tickets
            .Where(ticket => ticket.Id == id && ticket.Status == TicketStatuses.Pending)
            .Where(ticket => ticket.AppUserTickets.Any(link => link.AppUserId == appUserId))
            .FirstOrDefaultAsync(cancellationToken);

        if (ticket == null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        ticket.Description = description.Trim();
        ticket.Updated = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ticket;
    }

    public async Task<Ticket?> UpdateTicketStatusAsync(
        int id,
        int status,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _context.Tickets
            .FirstOrDefaultAsync(ticket => ticket.Id == id, cancellationToken);
        if (ticket == null)
        {
            return null;
        }

        ticket.Status = status;
        ticket.Updated = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return ticket;
    }

    public async Task<Ticket?> DeleteAsync(
        int id,
        string appUserId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var ticket = await _context.Tickets
            .Where(ticket => ticket.Id == id && ticket.Status == TicketStatuses.Pending)
            .Where(ticket => ticket.AppUserTickets.Any(link => link.AppUserId == appUserId))
            .FirstOrDefaultAsync(cancellationToken);

        if (ticket == null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        _context.Tickets.Remove(ticket);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ticket;
    }

    public async Task<IReadOnlyList<TicketStatusCountDto>> GetStatusCountsAsync(
        string? appUserId,
        CancellationToken cancellationToken = default)
    {
        var tickets = _context.Tickets.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(appUserId))
        {
            tickets = tickets.Where(ticket =>
                ticket.AppUserTickets.Any(link => link.AppUserId == appUserId));
        }

        var counts = await tickets
            .Where(ticket => ticket.Status >= TicketStatuses.Pending &&
                ticket.Status <= TicketStatuses.Completed)
            .GroupBy(ticket => ticket.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Status, item => item.Count, cancellationToken);

        return new[]
        {
            TicketStatuses.Pending,
            TicketStatuses.InProgress,
            TicketStatuses.Completed,
        }
        .Select(status => new TicketStatusCountDto
        {
            Status = TicketStatuses.ToApiValue(status),
            Count = counts.GetValueOrDefault(status),
        })
        .ToList();
    }

    private IQueryable<TicketRow> TicketRows(string? appUserId = null)
    {
        var tickets = _context.Tickets.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(appUserId))
        {
            tickets = tickets.Where(ticket =>
                ticket.AppUserTickets.Any(link => link.AppUserId == appUserId));
        }

        return tickets
            .Select(ticket => new TicketRow
            {
                Id = ticket.Id,
                NewProduct = ticket.NewProduct,
                Description = ticket.Description,
                Created = ticket.Created,
                Status = ticket.Status,
                Answer = ticket.Answer,
                Updated = ticket.Updated,
                CreatedBy = ticket.CreatedBy,
                FirmName = ticket.AppUserTickets
                    .SelectMany(link => link.AppUser.FirmUsers)
                    .Select(firmUser => firmUser.Firm.Name)
                    .FirstOrDefault(),
                ProductName = ticket.ProductTickets
                    .Select(link => link.Products.Name)
                    .FirstOrDefault(),
            });
    }

    private static TicketDto ToDto(TicketRow row)
    {
        return new TicketDto
        {
            Id = row.Id,
            NewProduct = row.NewProduct,
            Description = row.Description,
            Created = AsUtc(row.Created),
            Status = TicketStatuses.ToApiValue(row.Status),
            Answer = row.Answer,
            Updated = AsUtc(row.Updated),
            CreatedBy = row.CreatedBy ?? string.Empty,
            FirmName = row.FirmName,
            ProductName = row.ProductName,
        };
    }

    private static DateTime? AsUtc(DateTime? value)
    {
        return value.HasValue
            ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            : null;
    }

    private sealed class TicketRow
    {
        public int Id { get; set; }
        public bool NewProduct { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime? Created { get; set; }
        public int Status { get; set; }
        public string? Answer { get; set; }
        public DateTime? Updated { get; set; }
        public string? CreatedBy { get; set; }
        public string? FirmName { get; set; }
        public string? ProductName { get; set; }
    }
}
