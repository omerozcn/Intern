using System.Data;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Data;
using TicketSystem.Dtos.Common;
using TicketSystem.Dtos.Ticket;
using TicketSystem.Extensions;
using TicketSystem.Interfaces;
using TicketSystem.Models;

namespace TicketSystem.Repositories;

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

    public Task<PagedResult<TicketDto>> GetAllAsync(
        TicketListRequest request,
        CancellationToken cancellationToken = default)
    {
        return PageTicketsAsync(TicketRows(), request, cancellationToken);
    }

    public Task<PagedResult<TicketDto>> GetByUserIdAsync(
        string appUserId,
        TicketListRequest request,
        CancellationToken cancellationToken = default)
    {
        return PageTicketsAsync(TicketRows(appUserId), request, cancellationToken);
    }

    private static async Task<PagedResult<TicketDto>> PageTicketsAsync(
        IQueryable<TicketRow> rows,
        TicketListRequest request,
        CancellationToken cancellationToken)
    {
        if (request.FirmId is { } firmId)
        {
            rows = rows.Where(row => row.FirmId == firmId);
        }

        if (TicketStatuses.TryParseApiValue(request.Status, out var status))
        {
            rows = rows.Where(row => row.Status == status);
        }

        if (request.Search is not null)
        {
            rows = rows.Where(row =>
                row.Description.Contains(request.Search) ||
                (row.CreatedBy != null && row.CreatedBy.Contains(request.Search)) ||
                (row.FirmName != null && row.FirmName.Contains(request.Search)) ||
                (row.ProductName != null && row.ProductName.Contains(request.Search)));
        }

        // Pending tickets first, newest first inside each status.
        var page = await rows
            .OrderBy(row => row.Status)
            .ThenByDescending(row => row.Created)
            .ToPagedResultAsync(request, cancellationToken);

        return new PagedResult<TicketDto>
        {
            Items = page.Items.Select(ToDto).ToList(),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount
        };
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

    /// <summary>
    /// The firm the ticket belongs to, inherited from whoever raised it. Used to keep an
    /// administrator from answering another firm's ticket by guessing its id.
    /// </summary>
    public Task<int?> GetFirmIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _context.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.Id == id)
            .SelectMany(ticket => ticket.AppUserTickets)
            .SelectMany(link => link.AppUser.FirmUsers)
            .Select(firmUser => (int?)firmUser.FirmId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TicketStatusCountDto>> GetStatusCountsAsync(
        string? appUserId,
        int? firmId = null,
        CancellationToken cancellationToken = default)
    {
        var tickets = _context.Tickets.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(appUserId))
        {
            tickets = tickets.Where(ticket =>
                ticket.AppUserTickets.Any(link => link.AppUserId == appUserId));
        }

        // Keeps the dashboard totals consistent with the list an administrator can
        // actually open; global counts beside a firm-scoped list would just look broken.
        if (firmId is { } scopedFirmId)
        {
            tickets = tickets.Where(ticket =>
                ticket.AppUserTickets.Any(link =>
                    link.AppUser.FirmUsers.Any(firmUser => firmUser.FirmId == scopedFirmId)));
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
                // A ticket has no firm of its own; it inherits the requester's. Both the
                // id and the name come from the same row so the scope filter and the
                // label can never disagree.
                FirmId = ticket.AppUserTickets
                    .SelectMany(link => link.AppUser.FirmUsers)
                    .Select(firmUser => (int?)firmUser.FirmId)
                    .FirstOrDefault(),
                FirmName = ticket.AppUserTickets
                    .SelectMany(link => link.AppUser.FirmUsers)
                    .Select(firmUser => firmUser.Firm.Name)
                    .FirstOrDefault(),
                ProductName = ticket.ProductTickets
                    .Select(link => link.Product.Name)
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
        public int? FirmId { get; set; }
        public string? FirmName { get; set; }
        public string? ProductName { get; set; }
    }
}
