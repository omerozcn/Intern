using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Dtos.Ticket;
using TicketSystem.Extensions;
using TicketSystem.Interfaces;
using TicketSystem.Mappers;
using TicketSystem.Models;
using TicketSystem.Security;

namespace TicketSystem.Controllers;

[ApiController]
[Authorize]
[Route("api/Ticket")]
public sealed class TicketController : ControllerBase
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ILogger<TicketController> _logger;

    public TicketController(
        ITicketRepository ticketRepository,
        ILogger<TicketController> logger)
    {
        _ticketRepository = ticketRepository;
        _logger = logger;
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpGet("listTicket")]
    [ProducesResponseType<IReadOnlyList<TicketDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TicketDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        return Ok(await _ticketRepository.GetAllAsync(cancellationToken));
    }

    [Authorize(Roles = AppRoles.AdminOrUser)]
    [HttpGet("listById/{id:int}")]
    [ProducesResponseType<TicketDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketDto>> GetById(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetDtoByIdAsync(id, cancellationToken);
        if (ticket is null)
        {
            return TicketNotFound();
        }

        if (!User.IsInRole(AppRoles.Admin))
        {
            var appUserId = User.GetUserId();
            if (appUserId is null)
            {
                return MissingIdentityClaim();
            }

            if (!await _ticketRepository.IsOwnedByAsync(id, appUserId, cancellationToken))
            {
                return AccessDenied();
            }
        }

        return Ok(ticket);
    }

    [Authorize(Roles = AppRoles.User)]
    [HttpGet("listByUserId")]
    [ProducesResponseType<IReadOnlyList<TicketDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<TicketDto>>> GetByUserId(
        CancellationToken cancellationToken)
    {
        var appUserId = User.GetUserId();
        if (appUserId is null)
        {
            return MissingIdentityClaim();
        }

        return Ok(await _ticketRepository.GetByUserIdAsync(appUserId, cancellationToken));
    }

    [Authorize(Roles = AppRoles.User)]
    [HttpPost("createTicket")]
    [ProducesResponseType<TicketDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TicketDto>> Create(
        [FromBody] CreateTicketRequestDto ticketDto,
        CancellationToken cancellationToken)
    {
        // Length rules live on the DTO; only this cross-field rule needs code.
        if (!ticketDto.NewProduct && (!ticketDto.ProductId.HasValue || ticketDto.ProductId.Value <= 0))
        {
            ModelState.AddModelError(
                nameof(ticketDto.ProductId),
                "ProductId is required when newProduct is false.");
            return ValidationProblem(ModelState);
        }

        var appUserId = User.GetUserId();
        var firmId = User.GetFirmId();
        if (appUserId is null || !firmId.HasValue)
        {
            _logger.LogWarning("Ticket creation was rejected because identity claims were incomplete.");
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Required identity claims are missing");
        }

        var createdBy = User.GetDisplayName();
        var ticketModel = ticketDto.ToTicketFromCreateDto(
            string.IsNullOrWhiteSpace(createdBy) ? appUserId : createdBy);

        var createdTicket = await _ticketRepository.CreateAsync(
            ticketModel,
            appUserId,
            firmId.Value,
            ticketDto.ProductId,
            cancellationToken);

        if (createdTicket is null)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid product selection",
                detail: "The selected product is not assigned to the current user's firm.");
        }

        var response = await _ticketRepository.GetDtoByIdAsync(createdTicket.Id, cancellationToken)
            ?? createdTicket.ToTicketDto(User.GetFirmName(), null);

        return CreatedAtAction(nameof(GetById), new { id = createdTicket.Id }, response);
    }

    [Authorize(Roles = AppRoles.User)]
    [HttpPut("updateDescription/{id:int}")]
    [ProducesResponseType<TicketDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TicketDto>> UpdateDescription(
        [FromRoute] int id,
        [FromBody] UpdateTicketDescriptionRequestDto updateDto,
        CancellationToken cancellationToken)
    {
        var appUserId = User.GetUserId();
        if (appUserId is null)
        {
            return MissingIdentityClaim();
        }

        var existingTicket = await _ticketRepository.GetByIdAsync(id, cancellationToken);
        if (existingTicket is null)
        {
            return TicketNotFound();
        }

        if (!await _ticketRepository.IsOwnedByAsync(id, appUserId, cancellationToken))
        {
            return AccessDenied();
        }

        if (existingTicket.Status != TicketStatuses.Pending)
        {
            return NotEditable("Only pending tickets can be edited.");
        }

        var updatedTicket = await _ticketRepository.UpdateDescriptionAsync(
            id,
            appUserId,
            updateDto.Description,
            cancellationToken);

        if (updatedTicket is null)
        {
            return NotEditable("The ticket is no longer pending.");
        }

        return Ok(await _ticketRepository.GetDtoByIdAsync(id, cancellationToken));
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPut("updateTicket/{id:int}")]
    [ProducesResponseType<TicketDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketDto>> Update(
        [FromRoute] int id,
        [FromBody] UpdateTicketRequestDto updateDto,
        CancellationToken cancellationToken)
    {
        if (!TicketStatuses.TryParseApiValue(updateDto.Status, out var status))
        {
            return InvalidStatus(nameof(updateDto.Status));
        }

        if (status == TicketStatuses.Completed && string.IsNullOrWhiteSpace(updateDto.Answer))
        {
            ModelState.AddModelError(nameof(updateDto.Answer), "Answer is required.");
            return ValidationProblem(ModelState);
        }

        var updatedTicket = await _ticketRepository.UpdateAsync(
            id,
            updateDto.Answer,
            status,
            cancellationToken);
        if (updatedTicket is null)
        {
            return TicketNotFound();
        }

        return Ok(await _ticketRepository.GetDtoByIdAsync(id, cancellationToken));
    }

    [Authorize(Roles = AppRoles.User)]
    [HttpDelete("deleteTicket/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var appUserId = User.GetUserId();
        if (appUserId is null)
        {
            return MissingIdentityClaim();
        }

        var existingTicket = await _ticketRepository.GetByIdAsync(id, cancellationToken);
        if (existingTicket is null)
        {
            return TicketNotFound();
        }

        if (!await _ticketRepository.IsOwnedByAsync(id, appUserId, cancellationToken))
        {
            return AccessDenied();
        }

        if (existingTicket.Status != TicketStatuses.Pending)
        {
            return NotDeletable("Only pending tickets can be deleted.");
        }

        var deletedTicket = await _ticketRepository.DeleteAsync(id, appUserId, cancellationToken);
        if (deletedTicket is null)
        {
            return NotDeletable("The ticket is no longer pending.");
        }

        return NoContent();
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPut("updateStatus/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTicketStatus(
        [FromRoute] int id,
        [FromBody] UpdateStatusTicketDto updateDto,
        CancellationToken cancellationToken)
    {
        if (!TicketStatuses.TryParseApiValue(updateDto.Status, out var status))
        {
            return InvalidStatus(nameof(updateDto.Status));
        }

        if (status == TicketStatuses.Completed)
        {
            var existingTicket = await _ticketRepository.GetByIdAsync(id, cancellationToken);
            if (existingTicket is null)
            {
                return TicketNotFound();
            }

            if (string.IsNullOrWhiteSpace(existingTicket.Answer))
            {
                ModelState.AddModelError(
                    nameof(updateDto.Status),
                    "A response is required before a ticket can be completed.");
                return ValidationProblem(ModelState);
            }
        }

        var ticket = await _ticketRepository.UpdateTicketStatusAsync(id, status, cancellationToken);
        return ticket is null ? TicketNotFound() : NoContent();
    }

    [Authorize(Roles = AppRoles.AdminOrUser)]
    [HttpGet("ticketstatuscount")]
    [ProducesResponseType<IReadOnlyList<TicketStatusCountDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<TicketStatusCountDto>>> GetTicketStatusSummary(
        CancellationToken cancellationToken)
    {
        string? appUserId = null;
        if (!User.IsInRole(AppRoles.Admin))
        {
            appUserId = User.GetUserId();
            if (appUserId is null)
            {
                return MissingIdentityClaim();
            }
        }

        return Ok(await _ticketRepository.GetStatusCountsAsync(appUserId, cancellationToken));
    }

    private ObjectResult TicketNotFound()
    {
        return Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Ticket not found");
    }

    private ObjectResult AccessDenied()
    {
        return Problem(
            statusCode: StatusCodes.Status403Forbidden,
            title: "Ticket access denied");
    }

    private ObjectResult MissingIdentityClaim()
    {
        return Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Required identity claim is missing");
    }

    private ObjectResult NotEditable(string detail)
    {
        return Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Ticket cannot be edited",
            detail: detail);
    }

    private ObjectResult NotDeletable(string detail)
    {
        return Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Ticket cannot be deleted",
            detail: detail);
    }

    private ActionResult InvalidStatus(string fieldName)
    {
        ModelState.AddModelError(fieldName, "Status must be pending, inProgress, or completed.");
        return ValidationProblem(ModelState);
    }
}
