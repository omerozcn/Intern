namespace TicketSystem.Dtos.Product;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }

    /// <summary>
    /// Firms this product is assigned to. Populated by the list endpoint; empty on the
    /// responses to create and update, which describe the product alone.
    /// </summary>
    public IReadOnlyList<ProductFirmDto> Firms { get; set; } = [];
}
