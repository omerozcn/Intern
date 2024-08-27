using TicketSystem.Data;
using TicketSystem.Dtos.FirmProduct;
using TicketSystem.Interfaces;
using TicketSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace TicketSystem.Controllers
{
     [Route("api/Firmproduct")]
     [Authorize]
     [ApiController]
     public class FirmProductController : ControllerBase
     {
          private readonly ApplicationDbContext _context;
          private readonly UserManager<AppUser> _userManager;
          private readonly IFirmProductRepository _firmproductRepo;
          private readonly IProductRepository _productRepository;

          public FirmProductController(ApplicationDbContext context, UserManager<AppUser> user, IFirmProductRepository firmproductRepo, IProductRepository productRepo)
          {
               _userManager = user;
               _firmproductRepo = firmproductRepo;
               _productRepository = productRepo;
               _context = context;
          }

          [HttpGet("listfirmProduct")]
          public async Task<IActionResult> GetAll()
          {
               if (!ModelState.IsValid)
               {
                    return BadRequest(ModelState);
               }
               var firmproducts = await _firmproductRepo.GetAllAsyncs();

               var firmproductDto = firmproducts.Select(t => t.ToString()).ToList();

               return Ok(firmproducts);
          }

          [HttpPost("createfirmProduct")]
          public async Task<IActionResult> Create([FromBody] CreateFirmProductRequestDto firmproductDto)
          {
               if (!ModelState.IsValid)
               {
                    return BadRequest(ModelState);
               }

               try
               {
                    var firmproductModel = await _firmproductRepo.CreateAsync(firmproductDto);
                    return Ok(firmproductModel);
               }
               catch (Exception ex)
               {
                    if (ex.Message.Contains("already exists"))
                    {
                         return Conflict(ex.Message);
                    }
                    return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
               }
          }

          [HttpDelete("deletefirmProduct/{id}")]
          public async Task<IActionResult> Delete([FromRoute] int id)
          {
               Console.WriteLine($"Received request to delete FirmProduct with Id: {id}");

               var result = await _firmproductRepo.DeleteAsyncs(id);

               if (result == null)
               {
                    Console.WriteLine($"FirmProduct with Id: {id} not found in Delete method.");
                    return NotFound();
               }

               Console.WriteLine($"FirmProduct with Id: {id} successfully deleted in Delete method.");
               return Ok(result);
          }

          [HttpGet("listProductsByFirm/{firmname}")]
          public async Task<IActionResult> GetProductsByFirm([FromRoute] string firmname)
          {
               var firmproduct = await _firmproductRepo.GetFirmProductAsync(firmname);
               
               if(firmproduct == null)
               {
                    return NotFound();
               }

               return Ok(firmproduct);
          }
     }

}
