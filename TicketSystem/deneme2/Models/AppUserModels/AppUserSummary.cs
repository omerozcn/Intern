namespace TicketSystem.Models.AppUserModels
{
     public class AppUserSummary
     {
          public string Id { get; set; }
          public string? UserName { get; set; }
          public string? FirstName { get; set; }
          public string? LastName { get; set; }
          public string? Email { get; set; }
          public string? Role { get; set; }
          public string? FirmName { get; set; }
          public FirmUser FirmUser { get; set; }
     }
}
