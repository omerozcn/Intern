namespace TicketSystem.Dtos.FirmProduct;

public class FirmProductDto
{
    public int Id { get; set; }

    public int FirmId { get; set; }

    /// <summary>Populated when the assignment is read back with its related rows; null right after creation.</summary>
    public string? FirmName { get; set; }

    public int ProductId { get; set; }

    /// <summary>Populated when the assignment is read back with its related rows; null right after creation.</summary>
    public string? ProductName { get; set; }
}
