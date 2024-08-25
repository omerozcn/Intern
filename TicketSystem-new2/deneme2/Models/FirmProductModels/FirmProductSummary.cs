namespace TicketSystem.Models.FirmProductModels
{
     public class FirmProductSummary
     {
          public int Id { get; set; }
          public int FirmId { get; set; }
          public string FirmName { get; set; }
          public int ProductId { get; set; }
          public string ProductName { get; set; }
     }
}
