using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Dtos.Common;
using TicketSystem.Dtos.FirmProduct;
using TicketSystem.Interfaces;
using TicketSystem.Security;

namespace TicketSystem.Controllers;

[ApiController]
[Authorize]
[Route("api/firm-products")]
public sealed class FirmProductsController : ControllerBase
{
    private readonly IFirmProductRepository _firmProductRepository;

    public FirmProductsController(IFirmProductRepository firmProductRepository)
    {
        _firmProductRepository = firmProductRepository;
    }

    [Authorize(Policy = AppPolicies.SuperAdmin)]
    [HttpGet]
    [ProducesResponseType<PagedResult<FirmProductDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<FirmProductDto>>> GetAll(
        [FromQuery] PageRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _firmProductRepository.GetAllAsync(request, cancellationToken));
    }

    [Authorize(Policy = AppPolicies.SuperAdmin)]
    [HttpPost]
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

    [Authorize(Policy = AppPolicies.SuperAdmin)]
    [HttpDelete("{id:int}")]
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
}
