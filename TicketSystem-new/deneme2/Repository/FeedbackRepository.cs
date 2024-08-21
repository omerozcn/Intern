using TicketSystem.Data;
using TicketSystem.Interfaces;
using TicketSystem.Models;
using TicketSystem.Models.FeedbackModels;
using Microsoft.EntityFrameworkCore;

namespace TicketSystem.Repository
{
     public class FeedbackRepository : IFeedbackRepository
     {
          ApplicationDbContext _context;

          public FeedbackRepository(ApplicationDbContext context)
          {
               _context = context;
          }

          public async Task<Feedback> CreateAsync(Feedback feedbackModel)
          {
               await _context.Feedbacks.AddAsync(feedbackModel);
               await _context.SaveChangesAsync();
               return feedbackModel;
          }

          public async Task<List<FeedbackSummary>> GetAllFeedbackAsync()
          {
               var feedbacks = await _context.Feedbacks
                    .Select(t => new FeedbackSummary
                    {
                         Id = t.Id,
                         FeedbackContent = t.FeedbackContent,
                    }).ToListAsync();
               return feedbacks;
          }
     }
}
