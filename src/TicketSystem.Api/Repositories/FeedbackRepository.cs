using Microsoft.EntityFrameworkCore;
using TicketSystem.Data;
using TicketSystem.Dtos.Common;
using TicketSystem.Dtos.Feedback;
using TicketSystem.Extensions;
using TicketSystem.Interfaces;
using TicketSystem.Models;

namespace TicketSystem.Repositories;

public sealed class FeedbackRepository : IFeedbackRepository
{
    private readonly ApplicationDbContext _context;

    public FeedbackRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Feedback> CreateAsync(
        Feedback feedbackModel,
        CancellationToken cancellationToken = default)
    {
        await _context.Feedbacks.AddAsync(feedbackModel, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return feedbackModel;
    }

    public async Task<PagedResult<FeedbackDto>> GetAllFeedbackAsync(
        PageRequest request,
        CancellationToken cancellationToken = default)
    {
        var feedbacks = _context.Feedbacks.AsNoTracking();

        if (request.Search is not null)
        {
            feedbacks = feedbacks.Where(feedback => feedback.FeedbackContent.Contains(request.Search));
        }

        return await feedbacks
            .OrderByDescending(feedback => feedback.Id)
            .Select(feedback => new FeedbackDto
            {
                Id = feedback.Id,
                FeedbackContent = feedback.FeedbackContent,
            })
            .ToPagedResultAsync(request, cancellationToken);
    }
}
