using TicketSystem.Dtos.Feedback;
using TicketSystem.Models;

namespace TicketSystem.Mappers
{
     public static class FeedbackMappers
     {
          public static FeedbackDto ToFeedbackDto(this Feedback feedbackModel)
          {
               return new FeedbackDto
               {
                    Id = feedbackModel.Id,
                    FeedbackContet = feedbackModel.FeedbackContent,
               };
          }
          public static Feedback ToFeedbackFromCreateDTO(this CreateFeedbackRequestDto feedbackDto)
          {
               return new Feedback
               {
                    FeedbackContent = feedbackDto.FeedbackContent,
               };

          }
     }
}
