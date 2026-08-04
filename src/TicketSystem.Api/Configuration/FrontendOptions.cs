using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Configuration;

public sealed class FrontendOptions
{
    public const string SectionName = "Frontend";

    /// <summary>
    /// Base address of the SPA. Password reset links are built from this rather than
    /// from the request host, which is what makes host-header injection impossible.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    [Url]
    public string BaseUrl { get; set; } = "http://localhost:5173";
}
