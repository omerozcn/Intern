using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Dtos.FirmProduct
{
     public class FirmProductDto
     {
          public int Id { get; set; }

          public int FirmId { get; set; }

          public int ProductId { get; set; }
     }
}
