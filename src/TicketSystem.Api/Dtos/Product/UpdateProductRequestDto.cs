using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Dtos.Product;

public sealed class UpdateProductRequestDto
{
    private string? _name;

    /// <summary>
    /// Bounds mirror the Products.Name column so validation fails before the database does.
    /// </summary>
    [Required]
    [MinLength(2, ErrorMessage = "Service name must be at least 2 characters.")]
    [MaxLength(200, ErrorMessage = "Service name cannot be over 200 characters.")]
    public string? Name
    {
        get => _name;
        set => _name = value?.Trim();
    }
}
