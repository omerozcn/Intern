using TicketSystem.Models;
using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Dtos.Account
{
     public class RegisterDto
     {
          [Required]
          public string? UserName { get; set; }

          [Required]
          public string? LastName { get; set; }

          [Required]
          [EmailAddress]
          public string? Email { get; set; }

          [Required]
          public string? Password { get; set; }

          [Required]
          public string? Role { get; set; }

          [Required]
          public int FirmId { get; set; }
     }
}
