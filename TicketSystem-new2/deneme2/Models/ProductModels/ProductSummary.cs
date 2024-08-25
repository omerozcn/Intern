namespace TicketSystem.Models.ProductModels
{
     public class ProductSummary
     {
          public int Id { get; set; }
          public string Name { get; set; }
          public DateTime? BirthDate { get; set; }
          public string BirthDateFormatted => BirthDate?.ToString("yyyy-MM-dd HH:mm:ss");
     }
}
