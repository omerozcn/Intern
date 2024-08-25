using System.ComponentModel.DataAnnotations;
namespace TicketSystem.Dtos.Feedback

{
     public class CreateFeedbackRequestDto
     {
          [Required]
          public string FeedbackContent { get; set; }
     }
}
