using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketSystem.Models
{
     [Table("FirmProducts")]
     public class FirmProduct
     {
          [Key]
          public int Id { get; set; }
          [Required]
          public int FirmId { get; set; }
          [Required]
          public int ProductId { get; set; }
          public Firm Firm { get; set; }
          public Product Products { get; set; }
     }
}
