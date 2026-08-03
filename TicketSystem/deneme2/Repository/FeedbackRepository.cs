using Microsoft.EntityFrameworkCore;
using TicketSystem.Data;
using TicketSystem.Dtos.Feedback;
using TicketSystem.Interfaces;
using TicketSystem.Models;

namespace TicketSystem.Repository;

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

    public async Task<IReadOnlyList<FeedbackDto>> GetAllFeedbackAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Feedbacks
            .AsNoTracking()
            .OrderByDescending(feedback => feedback.Id)
            .Select(feedback => new FeedbackDto
            {
                Id = feedback.Id,
                FeedbackContent = feedback.FeedbackContent,
            })
            .ToListAsync(cancellationToken);
    }
}
