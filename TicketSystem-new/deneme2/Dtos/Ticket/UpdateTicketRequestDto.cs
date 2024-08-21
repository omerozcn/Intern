using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Dtos.Ticket
{
     public class UpdateTicketRequestDto
     {
          public int Id { get; set; }
          public string? Answer { get; set; }
          public int Status { get; set; }
     }
}
