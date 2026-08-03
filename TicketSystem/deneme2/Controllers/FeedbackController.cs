using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Dtos.Feedback;
using TicketSystem.Interfaces;
using TicketSystem.Mappers;
using TicketSystem.Security;

namespace TicketSystem.Controllers;

[ApiController]
[Authorize]
[Route("api/Feedback")]
public sealed class FeedbackController : ControllerBase
{
    private readonly IFeedbackRepository _feedbackRepository;

    public FeedbackController(IFeedbackRepository feedbackRepository)
    {
        _feedbackRepository = feedbackRepository;
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpGet("listFeedbacks")]
    [ProducesResponseType<IReadOnlyList<FeedbackDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FeedbackDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        return Ok(await _feedbackRepository.GetAllFeedbackAsync(cancellationToken));
    }

    [Authorize(Roles = AppRoles.User)]
    [HttpPost("createFeedback")]
    [ProducesResponseType<FeedbackDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FeedbackDto>> Create(
        [FromBody] CreateFeedbackRequestDto feedbackDto,
        CancellationToken cancellationToken)
    {
        var feedback = await _feedbackRepository.CreateAsync(
            feedbackDto.ToFeedbackFromCreateDTO(),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, feedback.ToFeedbackDto());
    }
}
