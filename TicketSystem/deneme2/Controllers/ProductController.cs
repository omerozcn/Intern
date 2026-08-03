using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Dtos.Product;
using TicketSystem.Interfaces;
using TicketSystem.Mappers;
using TicketSystem.Security;

namespace TicketSystem.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.Admin)]
[Route("api/Product")]
public sealed class ProductController : ControllerBase
{
    private readonly IProductRepository _productRepository;

    public ProductController(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    [HttpGet("listProduct")]
    [ProducesResponseType<IReadOnlyList<ProductDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        return Ok(await _productRepository.GetAllAsync(cancellationToken));
    }

    [HttpPost("createProduct")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductDto>> Create(
        [FromBody] CreateProductRequestDto productDto,
        CancellationToken cancellationToken)
    {
        var product = await _productRepository.CreateAsync(
            productDto.ToProductFromCreateDTO(),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, product.ToProductDto());
    }

    [HttpPut("updateProduct/{id:int}")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> Update(
        [FromRoute] int id,
        [FromBody] UpdateProductRequestDto updateDto,
        CancellationToken cancellationToken)
    {
        var product = await _productRepository.UpdateAsync(id, updateDto, cancellationToken);
        return product is null ? ProductNotFound() : Ok(product.ToProductDto());
    }

    [HttpDelete("deleteProduct/{id:int}")]
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
