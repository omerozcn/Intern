using TicketSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace TicketSystem.Dtos.Account
{
     public class NewNewUserDto
     {
          public string? Name { get; set; }
          public string? LastName { get; set; }
          public string? Email { get; set; }
          public string? Token { get; set; }
          public string? Role { get; set; }
          public int FirmId { get; set; }
          public FirmUser FirmUser { get; set; }
     }
}
