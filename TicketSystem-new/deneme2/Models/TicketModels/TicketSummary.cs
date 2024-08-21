namespace TicketSystem.Models.TicketModels
{
     public class TicketSummary
     {
          public int Id { get; set; }
          public string? Title { get; set; }
          public DateTime? Created { get; set; }
          public int Status { get; set; }
          public void AssignStatusValues()
          {
               Status = 1;
               Status = 2;
               Status = 3;
          }
          public string CreatedBy { get; set; }
          public string FirmName { get; set; }
     }
}
