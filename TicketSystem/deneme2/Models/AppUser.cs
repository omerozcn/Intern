using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Models
{
     public class AppUser : IdentityUser
     {
          public string? FirstName {get; set; }
          [Required]
          public string? LastName { get; set; }
          [Required]
          public string? Role { get; set; }
          public List<AppUserTicket> AppUserTickets { get; set; } = new List<AppUserTicket>();
          public List<FirmUser> FirmUsers { get; set; } = new List<FirmUser>();
     }
}
