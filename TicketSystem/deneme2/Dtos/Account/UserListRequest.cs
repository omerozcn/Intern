using Microsoft.AspNetCore.Mvc;
using TicketSystem.Dtos.Common;

namespace TicketSystem.Dtos.Account;

/// <summary>
/// Paging plus the role and firm filters, applied server side so they cover every page.
/// </summary>
public sealed class UserListRequest : PageRequest
{
    private string? _role;

    /// <summary>"Admin" or "User"; anything else means no filter.</summary>
    [FromQuery(Name = "role")]
    public string? Role
    {
        get => _role;
        set => _role = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [FromQuery(Name = "firmId")]
    public int? FirmId { get; set; }
}
