namespace TicketSystem.Dtos.Firm;

public sealed class FirmDto
{
    public int Id { get; set; }
    public string? Name { get; set; }

    /// <summary>Number of services assigned to this firm. Populated by the list endpoint.</summary>
    public int ProductCount { get; set; }

    /// <summary>True for the system firm, which cannot be renamed or deleted.</summary>
    public bool IsProtected { get; set; }
}
