using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Dtos.Ticket
{
    public class UpdateStatusTicketDto
    {
        [Required]
        public string Status { get; set; } = string.Empty;
    }
}
