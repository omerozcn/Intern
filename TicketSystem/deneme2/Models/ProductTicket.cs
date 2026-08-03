using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketSystem.Models;

[Table("ProductTickets")]
public class ProductTicket
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProductId { get; set; }

    [Required]
    public int TicketId { get; set; }

    // Populated by EF when the relation is loaded.
    public Ticket Ticket { get; set; } = null!;
    public Product Products { get; set; } = null!;
}
