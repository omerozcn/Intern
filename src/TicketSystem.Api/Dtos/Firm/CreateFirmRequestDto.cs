using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Dtos.Firm;

public sealed class CreateFirmRequestDto
{
    private string? _name;

    /// <summary>
    /// Bounds mirror the Firms.Name column so validation fails before the database does.
    /// </summary>
    [Required]
    [MinLength(2, ErrorMessage = "Firm name must be at least 2 characters.")]
    [MaxLength(200, ErrorMessage = "Firm name cannot be over 200 characters.")]
    public string? Name
    {
        get => _name;
        set => _name = value?.Trim();
    }
}
