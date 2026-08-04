using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Dtos.Common;
using TicketSystem.Dtos.Product;
using TicketSystem.Extensions;
using TicketSystem.Interfaces;
using TicketSystem.Mappers;
using TicketSystem.Security;

namespace TicketSystem.Controllers;

[ApiController]
[Authorize]
[Route("api/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly IProductRepository _productRepository;
    private readonly IFirmProductRepository _firmProductRepository;

    public ProductsController(
        IProductRepository productRepository,
        IFirmProductRepository firmProductRepository)
    {
        _productRepository = productRepository;
        _firmProductRepository = firmProductRepository;
    }

    /// <summary>Products assigned to the signed-in user's firm.</summary>
    [Authorize(Roles = AppRoles.User)]
    [HttpGet("mine")]
    [ProducesResponseType<IReadOnlyList<CurrentUserProductDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<CurrentUserProductDto>>> GetMine(
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

    [Authorize(Roles = AppRoles.Admin)]
    [HttpGet]
    [ProducesResponseType<PagedResult<ProductDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetAll(
        [FromQuery] PageRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _productRepository.GetAllAsync(request, cancellationToken));
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    [ProducesResponseType<ProductDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductDto>> Create(
        [FromBody] CreateProductRequestDto productDto,
        CancellationToken cancellationToken)
    {
        if (await _productRepository.GetByNameAsync(productDto.Name!, cancellationToken) is not null)
        {
            return DuplicateName();
        }

        var product = await _productRepository.CreateAsync(
            productDto.ToProductFromCreateDTO(),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, product.ToProductDto());
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPut("{id:int}")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDto>> Update(
        [FromRoute] int id,
        [FromBody] UpdateProductRequestDto updateDto,
        CancellationToken cancellationToken)
    {
        // Existence first: a missing product must answer 404, not 409 for a name clash
        // with some other product. FirmsController.Update follows the same order.
        if (await _productRepository.GetByIdAsync(id, cancellationToken) is null)
        {
            return ProductNotFound();
        }

        var duplicate = await _productRepository.GetByNameAsync(updateDto.Name!, cancellationToken);
        if (duplicate is not null && duplicate.Id != id)
        {
            return DuplicateName();
        }

        var product = await _productRepository.UpdateAsync(id, updateDto, cancellationToken);
        return product is null ? ProductNotFound() : Ok(product.ToProductDto());
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var existingProduct = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (existingProduct is null)
        {
            return ProductNotFound();
        }

        if (await _productRepository.HasTicketHistoryAsync(id, cancellationToken))
        {
            return ReferencedByTicketHistory();
        }

        // DeleteAsync re-checks inside a serializable transaction, so a product that
        // gained ticket history since the check above still fails safely.
        var deletedProduct = await _productRepository.DeleteAsync(id, cancellationToken);
        return deletedProduct is null ? ReferencedByTicketHistory() : NoContent();
    }

    private ObjectResult DuplicateName()
    {
        return Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Service name already exists",
            detail: "Another service is already registered with this name.");
    }

    private ObjectResult ProductNotFound()
    {
        return Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Product not found");
    }

    private ObjectResult ReferencedByTicketHistory()
    {
        return Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Product is referenced by ticket history",
            detail: "Products referenced by ticket history cannot be deleted.");
    }
}
