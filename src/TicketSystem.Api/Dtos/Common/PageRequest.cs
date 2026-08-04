using Microsoft.AspNetCore.Mvc;

namespace TicketSystem.Dtos.Common;

/// <summary>
/// Query string parameters shared by every paginated list endpoint. Values are clamped rather
/// than rejected so a caller can never ask for an unbounded page.
/// </summary>
public class PageRequest
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    private int _page = 1;
    private int _pageSize = DefaultPageSize;
    private string? _search;

    [FromQuery(Name = "page")]
    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    [FromQuery(Name = "pageSize")]
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => value
        };
    }

    /// <summary>Optional case-insensitive filter; the meaning of the match is per endpoint.</summary>
    [FromQuery(Name = "search")]
    public string? Search
    {
        get => _search;
        set => _search = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public int Skip => (Page - 1) * PageSize;
}
