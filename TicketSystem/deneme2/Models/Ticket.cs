using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.ObjectPool;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketSystem.Models
{
     [Table("Tickets")]
     public class Ticket
     {
          [Key]
          public int Id { get; set; }
          [Required]
          public string? Title { get; set; }
          [Required]
          public string? Description { get; set; }
          public int Status { get; set; }
          public void AssignStatusValues()
          {
               Status = 1;
               Status = 2;
               Status = 3;
          }
          public string? Answer { get; set; }
          public DateTime? Created { get; set; } = DateTime.Now;
          public DateTime? Updated { get; set; } = DateTime.Now;
          public string CreatedBy { get; set; }
          public List<ProductTicket> ProductTickets { get; set; } = new List<ProductTicket>();
          public List<AppUserTicket> AppUserTickets { get; set; } = new List<AppUserTicket>();


     }
}
