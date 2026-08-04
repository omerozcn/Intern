using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required(AllowEmptyStrings = false)]
    public string Issuer { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Length is checked by a startup rule rather than an attribute, because in
    /// Development an ephemeral key is generated when this is left empty.
    /// </summary>
    public string SigningKey { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int ExpirationMinutes { get; set; } = 60;

    [Range(0, 300)]
    public int ClockSkewSeconds { get; set; } = 30;
}
