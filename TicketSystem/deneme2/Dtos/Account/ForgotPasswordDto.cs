using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Dtos.Account;

public sealed class ForgotPasswordDto
{
    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string Email { get; set; } = string.Empty;
}
