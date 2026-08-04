using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Dtos.Account;

public sealed class UpdateDto
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(64)]
    public string Role { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int FirmId { get; set; }
}
