using TicketSystem.Data;
using TicketSystem.Dtos.Product;
using TicketSystem.Interfaces;
using TicketSystem.Mappers;
using TicketSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace TicketSystem.Controllers
{
     [Route("api/Product")]
     [Authorize]
     [ApiController]
     public class ProductController : ControllerBase
     {
          private readonly UserManager<AppUser> _userManager;
          private readonly IProductRepository _productRepo;
          public ProductController(ApplicationDbContext context, IProductRepository productRepo, UserManager<AppUser> userManager)
          {
               _userManager = userManager;
               _productRepo = productRepo;
          }

          [HttpGet("listProduct")]
          public async Task<IActionResult> GetAll()
          {
               if (!ModelState.IsValid)
               {
                    return BadRequest(ModelState);
               }
               var product = await _productRepo.GetAllAsync();

               return Ok(product);
          }

          [HttpPost("createProduct")]
          public async Task<IActionResult> Create([FromBody] CreateProductRequestDto productDto)
          {
               if (!ModelState.IsValid)
               {
                    return BadRequest(ModelState);
               }

               var productModel = productDto.ToProductFromCreateDTO();
               await _productRepo.CreateAsync(productModel);

               return Ok();
          }

          [HttpPut("updateProduct/{id:int}")]
          public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateProductRequestDto updateDto)
          {
               if (!ModelState.IsValid)
               {
                    return BadRequest(ModelState);
               }
               var productModel = await _productRepo.UpdateAsync(id, updateDto);

               if (productModel == null)
               {
                    return NotFound();
               }

               return Ok(productModel.ToProductDto());
          }

          [HttpDelete("deleteProduct/{id}")]
          public async Task<IActionResult> Delete([FromRoute] int id)
          {
               if (!ModelState.IsValid)
               {
                    return BadRequest(ModelState);
               }
               var productModel = await _productRepo.DeleteAsync(id);

               if (productModel == null)
               {
                    return NotFound();
               }

               return NoContent();
          }
     }
}
