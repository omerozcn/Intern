using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Dtos.Ticket;

public sealed class UpdateTicketStatusRequestDto
{
    [Required]
    public string Status { get; set; } = string.Empty;
}
