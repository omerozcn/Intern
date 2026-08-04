using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketSystem.Models;

[Table("FirmProducts")]
public class FirmProduct
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int FirmId { get; set; }

    [Required]
    public int ProductId { get; set; }

    // Populated by EF when the relation is loaded.
    public Firm Firm { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
