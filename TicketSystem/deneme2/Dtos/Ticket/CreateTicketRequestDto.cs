using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Dtos.Ticket
{
     public class CreateTicketRequestDto
     {
          [Required]
          [MinLength(5, ErrorMessage = "Decription must be 5 characters")]
          [MaxLength(280, ErrorMessage = "Decription cannot be over 280 characters")]
          public string? Description { get; set; }
          public bool NewProduct { get; set; }
          public string? CreatedBy { get; set; }
          public int Status { get; set; }
          public string FirmName { get; set; }
          public int ProductId { get; set; }
     }
}
