using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Dtos.FirmProduct;
using TicketSystem.Dtos.Product;
using TicketSystem.Extensions;
using TicketSystem.Interfaces;
using TicketSystem.Security;

namespace TicketSystem.Controllers;

[ApiController]
[Authorize]
[Route("api/Firmproduct")]
public sealed class FirmProductController : ControllerBase
{
    private readonly IFirmProductRepository _firmProductRepository;

    public FirmProductController(IFirmProductRepository firmProductRepository)
    {
        _firmProductRepository = firmProductRepository;
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpGet("listfirmProduct")]
    [ProducesResponseType<IReadOnlyList<FirmProductDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FirmProductDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        return Ok(await _firmProductRepository.GetAllAsync(cancellationToken));
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost("createfirmProduct")]
    [ProducesResponseType<FirmProductDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FirmProductDto>> Create(
        [FromBody] CreateFirmProductRequestDto firmProductDto,
        CancellationToken cancellationToken)
    {
        var result = await _firmProductRepository.CreateAsync(firmProductDto, cancellationToken);

        return result.Status switch
        {
            FirmProductCreateStatus.Created =>
                StatusCode(StatusCodes.Status201Created, result.Assignment),
            FirmProductCreateStatus.DuplicateAssignment => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Firm-product assignment already exists",
                detail: "The requested firm-product assignment already exists."),
            _ => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid firm-product assignment",
                detail: "The specified firm or product does not exist."),
        };
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpDelete("deletefirmProduct/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var deleted = await _firmProductRepository.DeleteAsync(id, cancellationToken);
        return deleted is null
            ? Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Firm-product assignment not found")
            : NoContent();
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpGet("listProductsByFirm/{firmname}")]
    [ProducesResponseType<IReadOnlyList<FirmProductDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FirmProductDto>>> GetProductsByFirm(
        [FromRoute] string firmname,
        CancellationToken cancellationToken)
    {
        return Ok(await _firmProductRepository.GetFirmProductAsync(firmname, cancellationToken));
    }

    [Authorize(Roles = AppRoles.User)]
    [HttpGet("listProductsForCurrentUser")]
    [ProducesResponseType<IReadOnlyList<CurrentUserProductDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<CurrentUserProductDto>>> GetProductsForCurrentUser(
        CancellationToken cancellationToken)
    {
        var firmId = User.GetFirmId();

        if (!firmId.HasValue)
        {
            // Older tokens may predate the firmId claim, so fall back to the stored assignment.
            var appUserId = User.GetUserId();
            if (appUserId is null)
            {
                return Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Required identity claim is missing");
            }

            firmId = await _firmProductRepository.GetFirmIdForUserAsync(appUserId, cancellationToken);
        }

        if (!firmId.HasValue)
        {
            return Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "No firm is assigned to the current user");
        }

        return Ok(await _firmProductRepository.GetProductsByFirmIdAsync(firmId.Value, cancellationToken));
    }
}
