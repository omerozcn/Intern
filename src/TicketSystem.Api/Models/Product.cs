using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketSystem.Models;

[Table("Products")]
public class Product
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public DateTime? BirthDate { get; set; } = DateTime.UtcNow;

    public List<ProductTicket> ProductTickets { get; set; } = [];
    public List<FirmProduct> FirmProducts { get; set; } = [];
}
