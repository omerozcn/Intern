using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketSystem.Models;

[Table("Tickets")]
public class Ticket
{
    [Key]
    public int Id { get; set; }

    [Required]
    public bool NewProduct { get; set; }

    [Required]
    [StringLength(280, MinimumLength = 30)]
    public string Description { get; set; } = string.Empty;

    public int Status { get; set; } = TicketStatuses.Pending;

    [MaxLength(2000)]
    public string? Answer { get; set; }

    public DateTime? Created { get; set; } = DateTime.UtcNow;
    public DateTime? Updated { get; set; }

    [MaxLength(256)]
    public string? CreatedBy { get; set; }

    public List<ProductTicket> ProductTickets { get; set; } = [];
    public List<AppUserTicket> AppUserTickets { get; set; } = [];
}

public static class TicketStatuses
{
    public const int Pending = 1;
    public const int InProgress = 2;
    public const int Completed = 3;

    public static string ToApiValue(int status) => status switch
    {
        Pending => "pending",
        InProgress => "inProgress",
        Completed => "completed",
        _ => "pending"
    };

    public static bool TryParseApiValue(string? status, out int value)
    {
        value = status?.Trim().ToLowerInvariant() switch
        {
            "pending" => Pending,
            "inprogress" => InProgress,
            "completed" => Completed,
            _ => 0
        };

        return value != 0;
    }
}
