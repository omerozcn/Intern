using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketSystem.Models;

[Table("Feedbacks")]
public class Feedback
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(2000)]
    public string FeedbackContent { get; set; } = string.Empty;
}
