using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketSystem.Models;

[Table("AppUserTickets")]
public class AppUserTicket
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string AppUserId { get; set; } = string.Empty;

    [Required]
    public int TicketId { get; set; }

    // Populated by EF when the relation is loaded.
    public AppUser AppUser { get; set; } = null!;
    public Ticket Ticket { get; set; } = null!;
}
