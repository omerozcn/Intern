using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketSystem.Models;

[Table("FirmUsers")]
public class FirmUser
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int FirmId { get; set; }

    [Required]
    public string AppUserId { get; set; } = string.Empty;

    // Populated by EF when the relation is loaded.
    public Firm Firm { get; set; } = null!;
    public AppUser AppUser { get; set; } = null!;
}
