using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketSystem.Models
{
     [Table("FirmUsers")]
     public class FirmUser
     {
          [Key]
          [Required]
          public int Id { get; set; }
          [Required]
          public int FirmId { get; set; }
          [Required]
          public string? AppUserId { get; set; }
          public Firm Firm { get; set; }
          public AppUser AppUser { get; set; }
     }
}
