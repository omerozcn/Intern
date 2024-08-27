using TicketSystem.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace TicketSystem.Dtos.Account
{
     public class RegisterDto
     {
          [Required]
          public string? FirstName { get; set; }

          [Required]
          public string? LastName { get; set; }

          [Required]
          [EmailAddress]
          public string? Email { get; set; }

          [Required]
          public string? Password { get; set; }

          [Required]
          public string? Role { get; set; }

          public int FirmId { get; set; }
     }
}
