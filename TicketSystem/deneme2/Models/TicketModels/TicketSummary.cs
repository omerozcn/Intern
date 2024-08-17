namespace TicketSystem.Models.TicketModels
{
     public class TicketSummary
     {
          public int Id { get; set; }
          public string? Title { get; set; }
          public DateTime? Created { get; set; }
          public string CreatedBy { get; set; }
     }
}
