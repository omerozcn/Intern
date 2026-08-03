using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Dtos.Account;

public sealed class ResetPasswordDto
{
    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(4096)]
    public string Token { get; set; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 12)]
    public string NewPassword { get; set; } = string.Empty;
}
