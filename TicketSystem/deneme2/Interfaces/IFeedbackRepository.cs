using TicketSystem.Models;
using TicketSystem.Models.FeedbackModels;

namespace TicketSystem.Interfaces
{
     public interface IFeedbackRepository
     {
          Task<Feedback> CreateAsync(Feedback feedbackModel);
          Task<List<FeedbackSummary>> GetAllFeedbackAsync();
     }
}
