using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Models;

public class AppUser : IdentityUser
{
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    // The account's role lives only in the ASP.NET Identity role tables.
    public List<AppUserTicket> AppUserTickets { get; set; } = [];
    public List<FirmUser> FirmUsers { get; set; } = [];
}
