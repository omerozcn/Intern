using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TicketSystem.Dtos.Common;

namespace TicketSystem.Dtos.Ticket;

/// <summary>
/// Paging plus the status filter. Filtering has to happen server side: applying it to the
/// current page only would hide matching rows that live on other pages.
/// </summary>
public sealed class TicketListRequest : PageRequest
{
    private string? _status;

    /// <summary>"pending", "inProgress" or "completed"; anything else means no filter.</summary>
    [FromQuery(Name = "status")]
    public string? Status
    {
        get => _status;
        set => _status = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// Restricts the list to one firm's tickets.
    ///
    /// [BindNever] on purpose: this is set by the controller from the caller's claims and
    /// must never be supplied by the client. Leaving it bindable would let an
    /// administrator widen or redirect their own scope through the query string.
    /// </summary>
    [BindNever]
    public int? FirmId { get; set; }
}
