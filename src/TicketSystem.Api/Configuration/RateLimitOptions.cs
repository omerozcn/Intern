namespace TicketSystem.Configuration;

public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    public int GlobalPermitLimit { get; set; } = 120;
    public int GlobalWindowSeconds { get; set; } = 60;
    public int LoginPermitLimit { get; set; } = 5;
    public int LoginWindowSeconds { get; set; } = 60;
    public int PasswordPermitLimit { get; set; } = 5;
    public int PasswordWindowSeconds { get; set; } = 900;
}
