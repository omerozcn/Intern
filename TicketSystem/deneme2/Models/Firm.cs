using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketSystem.Models;

[Table("Firms")]
public class Firm
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public List<FirmUser> FirmUsers { get; set; } = [];
    public List<FirmProduct> FirmProducts { get; set; } = [];
}
