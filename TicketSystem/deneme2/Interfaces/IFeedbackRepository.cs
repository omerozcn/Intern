using TicketSystem.Dtos.Common;
using TicketSystem.Dtos.Feedback;
using TicketSystem.Models;

namespace TicketSystem.Interfaces;

public interface IFeedbackRepository
{
    Task<Feedback> CreateAsync(Feedback feedbackModel, CancellationToken cancellationToken = default);
    Task<PagedResult<FeedbackDto>> GetAllFeedbackAsync(PageRequest request, CancellationToken cancellationToken = default);
}
