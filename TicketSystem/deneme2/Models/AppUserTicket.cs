using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketSystem.Models
{
     [Table("AppUserTickets")]
     public class AppUserTicket
     {
          [Key]
          public int Id { get; set; }
          [Required]
          public string AppUserId { get; set; }
          [Required]
          public int TicketId { get; set; }
          public AppUser AppUser { get; set; }
          public Ticket Ticket { get; set; }
     }
}
