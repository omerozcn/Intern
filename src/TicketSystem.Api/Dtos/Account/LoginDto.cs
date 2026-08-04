using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Dtos.Account;

public sealed class LoginDto
{
    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string Password { get; set; } = string.Empty;
}
