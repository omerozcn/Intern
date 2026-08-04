namespace TicketSystem.Dtos.Account;

public sealed class AuthSessionDto
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public ProfileDto User { get; set; } = new();
}
