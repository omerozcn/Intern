using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Dtos.Firm;
using TicketSystem.Interfaces;
using TicketSystem.Mappers;
using TicketSystem.Models;
using TicketSystem.Security;

namespace TicketSystem.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.Admin)]
[Route("api/Firm")]
public sealed class FirmController : ControllerBase
{
    /// <summary>The system firm that owns the platform; it must never be renamed or removed.</summary>
    private const string ProtectedFirmName = "TURKUVAZ";

    private readonly IFirmRepository _firmRepository;

    public FirmController(IFirmRepository firmRepository)
    {
        _firmRepository = firmRepository;
    }

    [HttpGet("listFirm")]
    [ProducesResponseType<IReadOnlyList<FirmDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FirmDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        return Ok(await _firmRepository.GetAllAsync(cancellationToken));
    }

    [HttpGet("listById/{id:int}")]
    [ProducesResponseType<FirmDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FirmDto>> GetById(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var firm = await _firmRepository.GetByIdAsync(id, cancellationToken);
        return firm is null ? FirmNotFound() : Ok(firm.ToFirmDto());
    }

    [HttpPost("createFirm")]
    [ProducesResponseType<FirmDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FirmDto>> Create(
        [FromBody] CreateFirmRequestDto firmDto,
        CancellationToken cancellationToken)
    {
        if (IsReservedName(firmDto.Name))
        {
            return ProtectedFirm();
        }

        var firm = await _firmRepository.CreateAsync(
            firmDto.ToFirmFromCreateDTO(),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = firm.Id }, firm.ToFirmDto());
    }

    [HttpPut("updateFirm/{id:int}")]
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

        if (IsProtectedFirm(existingFirm) || IsReservedName(updateDto.Name))
        {
            return ProtectedFirm();
        }

        var firm = await _firmRepository.UpdateAsync(id, updateDto, cancellationToken);
        return firm is null ? FirmNotFound() : Ok(firm.ToFirmDto());
    }

    [HttpDelete("deleteFirm/{id:int}")]
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
            return ProtectedFirm();
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

    private ObjectResult ProtectedFirm()
    {
        return Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Protected firm",
            detail: $"{ProtectedFirmName} is a protected system firm.");
    }

    private static bool IsProtectedFirm(Firm firm)
    {
        return IsReservedName(firm.Name);
    }

    private static bool IsReservedName(string? name)
    {
        return string.Equals(name?.Trim(), ProtectedFirmName, StringComparison.OrdinalIgnoreCase);
    }
}
