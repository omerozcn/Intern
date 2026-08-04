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
}
