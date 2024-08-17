using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Dtos.Ticket
{
     public class UpdateTicketRequestDto
     {
          [Required]
          [MinLength(5, ErrorMessage = "Title must be 5 characters")]
          [MaxLength(280, ErrorMessage = "Title cannot be over 280 characters")]
          public string? Title { get; set; }
          [Required]
          [MinLength(5, ErrorMessage = "Decription must be 5 characters")]
          [MaxLength(280, ErrorMessage = "Decription cannot be over 280 characters")]
          public string? Description { get; set; }
     }
}
