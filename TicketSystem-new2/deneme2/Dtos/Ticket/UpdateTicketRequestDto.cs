using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices.JavaScript;

namespace TicketSystem.Dtos.Ticket
{
     public class UpdateTicketRequestDto
     {
          public int Id { get; set; }
          public string? Answer { get; set; }
          public DateTime Update { get; set; } = DateTime.Now;
          public int Status { get; set; }
     }
}
