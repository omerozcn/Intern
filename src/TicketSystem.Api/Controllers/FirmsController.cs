using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Dtos.Common;
using TicketSystem.Dtos.Firm;
using TicketSystem.Interfaces;
using TicketSystem.Mappers;
using TicketSystem.Models;
using TicketSystem.Security;

namespace TicketSystem.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.Admin)]
[Route("api/firms")]
public sealed class FirmsController : ControllerBase
{
    private readonly IFirmRepository _firmRepository;

    public FirmsController(IFirmRepository firmRepository)
    {
        _firmRepository = firmRepository;
    }

    [HttpGet]
    [ProducesResponseType<PagedResult<FirmDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<FirmDto>>> GetAll(
        [FromQuery] PageRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _firmRepository.GetAllAsync(request, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<FirmDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FirmDto>> GetById(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var firm = await _firmRepository.GetByIdAsync(id, cancellationToken);
        return firm is null ? FirmNotFound() : Ok(firm.ToFirmDto());
    }

    [HttpPost]
    [ProducesResponseType<FirmDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FirmDto>> Create(
        [FromBody] CreateFirmRequestDto firmDto,
        CancellationToken cancellationToken)
    {
        if (ProtectedFirm.IsProtectedName(firmDto.Name))
        {
            return ProtectedFirmConflict();
        }

        if (await _firmRepository.GetByNameAsync(firmDto.Name!, cancellationToken) is not null)
        {
            return DuplicateName();
        }

        var firm = await _firmRepository.CreateAsync(
            firmDto.ToFirmFromCreateDTO(),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = firm.Id }, firm.ToFirmDto());
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<FirmDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FirmDto>> Update(
        [FromRoute] int id,
        [FromBody] UpdateFirmRequestDto updateDto,
        CancellationToken cancellationToken)
    {
        var existingFirm = await _firmRepository.GetByIdAsync(id, cancellationToken);
        if (existingFirm is null)
        {
            return FirmNotFound();
        }

        if (IsProtectedFirm(existingFirm) || ProtectedFirm.IsProtectedName(updateDto.Name))
        {
            return ProtectedFirmConflict();
        }

        var duplicate = await _firmRepository.GetByNameAsync(updateDto.Name!, cancellationToken);
        if (duplicate is not null && duplicate.Id != id)
        {
            return DuplicateName();
        }

        var firm = await _firmRepository.UpdateAsync(id, updateDto, cancellationToken);
        return firm is null ? FirmNotFound() : Ok(firm.ToFirmDto());
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var existingFirm = await _firmRepository.GetByIdAsync(id, cancellationToken);
        if (existingFirm is null)
        {
            return FirmNotFound();
        }

        if (IsProtectedFirm(existingFirm))
        {
            return ProtectedFirmConflict();
        }

        if (await _firmRepository.HasTicketHistoryAsync(id, cancellationToken))
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Firm is referenced by ticket history",
                detail: "Firms referenced by ticket history cannot be deleted.");
        }

        if (await _firmRepository.HasUsersAsync(id, cancellationToken))
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Firm has assigned users",
                detail: "Firms with active users cannot be deleted.");
        }

        // DeleteAsync re-checks inside a serializable transaction, so a firm that
        // gained a dependent record since the checks above still fails safely.
        var deletedFirm = await _firmRepository.DeleteAsync(id, cancellationToken);
        if (deletedFirm is null)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Firm is still referenced",
                detail: "The firm gained a dependent record and cannot be deleted.");
        }

        return NoContent();
    }

    private ObjectResult FirmNotFound()
    {
        return Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Firm not found");
    }

    private ObjectResult DuplicateName()
    {
        return Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Firm name already exists",
            detail: "Another firm is already registered with this name.");
    }

    private ObjectResult ProtectedFirmConflict()
    {
        return Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Protected firm",
            detail: $"{ProtectedFirm.Name} is a protected system firm.");
    }

    private static bool IsProtectedFirm(Firm firm)
    {
        return ProtectedFirm.IsProtectedName(firm.Name);
    }
}
