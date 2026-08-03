using Microsoft.EntityFrameworkCore;
using TicketSystem.Dtos.Common;

namespace TicketSystem.Extensions;

public static class QueryableExtensions
{
    /// <summary>
    /// Runs the count and the page in one place. The caller must apply a deterministic
    /// <c>OrderBy</c> first, otherwise SQL Server may return rows in a different order per page.
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        PageRequest request,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<T>.Create(items, request, totalCount);
    }
}
