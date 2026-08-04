using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Dtos.Ticket;

public sealed class UpdateTicketRequestDto
{
    [StringLength(2000, ErrorMessage = "Answer cannot exceed 2000 characters.")]
    public string? Answer { get; set; }

    [Required]
    public string Status { get; set; } = string.Empty;
}
