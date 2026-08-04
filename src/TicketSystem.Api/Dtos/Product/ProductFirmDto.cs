namespace TicketSystem.Dtos.Product;

/// <summary>
/// A firm a product is assigned to, carried inside <see cref="ProductDto"/> so clients do not
/// have to join the product list against the firm-product assignment list themselves.
/// </summary>
public sealed class ProductFirmDto
{
    /// <summary>Identifier of the assignment row, needed to remove the assignment.</summary>
    public int RelationId { get; set; }

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
