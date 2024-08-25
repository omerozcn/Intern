namespace TicketSystem.Helpers
{
     public class QueryObject
     {
          public string? Name { get; set; } = null;
          public string? Description { get; set; } = null;
          public int PageNumber { get; set; } = 1;
          public int PageSize { get; set; } = 20;
     }
}
