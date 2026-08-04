using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Configuration;

public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    [Range(1, int.MaxValue)]
    public int GlobalPermitLimit { get; set; } = 120;

    [Range(1, int.MaxValue)]
    public int GlobalWindowSeconds { get; set; } = 60;

    [Range(1, int.MaxValue)]
    public int LoginPermitLimit { get; set; } = 5;

    [Range(1, int.MaxValue)]
    public int LoginWindowSeconds { get; set; } = 60;

    [Range(1, int.MaxValue)]
    public int PasswordPermitLimit { get; set; } = 5;

    [Range(1, int.MaxValue)]
    public int PasswordWindowSeconds { get; set; } = 900;
}
