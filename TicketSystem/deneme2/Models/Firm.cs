using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketSystem.Models
{
     [Table("Firms")]
     public class Firm
     {
          [Key]
          public int Id { get; set; }
          [Required]
          public string Name { get; set; }
          public List<FirmUser> FirmUsers { get; set; } = new List<FirmUser>();
          public List<FirmProduct> FirmProducts { get; set; } = new List<FirmProduct>();
     }
}
