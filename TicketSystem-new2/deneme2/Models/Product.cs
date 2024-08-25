using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Models
{
     [Table("Products")]
     public class Product
     {
          [Key]
          public int Id { get; set; }
          [Required]
          public string Name { get; set; }
          public DateTime? BirthDate { get; set; } = DateTime.Now;
          public List<ProductTicket> ProductTickets { get; set; } = new List<ProductTicket>();
          public List<FirmProduct> FirmProducts { get; set; } = new List<FirmProduct>();
     }
}
