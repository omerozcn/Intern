using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TicketSystem.Dtos.Ticket;

public class CreateTicketRequestDto
{
    private string _description = string.Empty;

    /// <summary>
    /// Trimmed on assignment so validation runs against the value that is actually stored.
    /// </summary>
    [Required]
    [StringLength(280, MinimumLength = 30, ErrorMessage = "Description must be between 30 and 280 characters.")]
    public string Description
    {
        get => _description;
        set => _description = value?.Trim() ?? string.Empty;
    }

    [JsonRequired]
    public bool NewProduct { get; set; }

    public int? ProductId { get; set; }
}
