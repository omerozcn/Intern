using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Dtos.FirmProduct;

public sealed class CreateFirmProductRequestDto
{
    // [Required] on a non-nullable int always passes, so 0 and negative values used to
    // reach the repository. Range is what actually rejects them, matching RegisterDto.
    [Range(1, int.MaxValue)]
    public int FirmId { get; set; }

    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }
}
