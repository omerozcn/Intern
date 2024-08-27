using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Dtos.Firm
{
     public class CreateFirmRequestDto
     {
          [Required]
          [MinLength(5, ErrorMessage = "Firm name must be 2 characters")]
          [MaxLength(280, ErrorMessage = "Firm name cannot be over 280 characters")]
          public string? Name { get; set; }
     }
}
