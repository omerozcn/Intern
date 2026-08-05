using TicketSystem.Dtos.Firm;

namespace TicketSystem.Dtos.Account;

public sealed class ProfileDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public FirmDto? Firm { get; set; }

    /// <summary>
    /// An administrator of the firm that owns the platform. Derived rather than stored,
    /// and sent explicitly so the client never has to infer privilege from a firm name.
    /// </summary>
    public bool IsSuperAdmin { get; set; }
}
