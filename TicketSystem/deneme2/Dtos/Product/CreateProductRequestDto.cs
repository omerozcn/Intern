using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Dtos.Product
{
     public class CreateProductRequestDto
     {
          [Required]
          [MinLength(5, ErrorMessage = "Service name must be 5 characters")]
          [MaxLength(280, ErrorMessage = "Service cannot be over 280 characters")]
          public string? Name { get; set; }
     }
}
