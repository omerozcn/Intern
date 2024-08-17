using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Dtos.Account
{
     public class UpdateDto
     {
          [Required]
          public string Id { get; set; }
          [Required]
          public string? Name { get; set; }
          [Required]
          public string? LastName { get; set; }
          [Required]
          [EmailAddress]
          public string? Email { get; set; }
          [Required]
          public string? Role { get; set; }
     }
}
