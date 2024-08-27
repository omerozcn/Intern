using TicketSystem.Data;
using TicketSystem.Models;
using TicketSystem.Dtos.Feedback;
using TicketSystem.Interfaces;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Mappers;

namespace TicketSystem.Controllers
{
     [Route("api/Feedback")]
     [ApiController]
     public class FeedbackController: ControllerBase
     {
          private readonly ApplicationDbContext _context;
          private readonly IFeedbackRepository _feedRepo;
          public FeedbackController(ApplicationDbContext context, IFeedbackRepository feedrRepo)
          {
               _context = context;
               _feedRepo = feedrRepo;
          }
          [HttpGet("listFeedbacks")]
          public async Task<IActionResult> GetAll()
          {
               var feecbacks = await _feedRepo.GetAllFeedbackAsync();

               var feedbackDto = feecbacks.Select(t => t.ToString()).ToList();

               return Ok(feecbacks);
          }

          [HttpPost("createFeedback")]
          public async Task<IActionResult> Create([FromBody] CreateFeedbackRequestDto feedbackDto)
          {
               var feedbackModel = feedbackDto.ToFeedbackFromCreateDTO();
               await _feedRepo.CreateAsync(feedbackModel);
               return Ok(feedbackModel);
          }
     }
}
