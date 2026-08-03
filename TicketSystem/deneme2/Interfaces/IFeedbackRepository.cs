using TicketSystem.Dtos.Feedback;
using TicketSystem.Models;

namespace TicketSystem.Interfaces;

public interface IFeedbackRepository
{
    Task<Feedback> CreateAsync(Feedback feedbackModel, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FeedbackDto>> GetAllFeedbackAsync(CancellationToken cancellationToken = default);
}
