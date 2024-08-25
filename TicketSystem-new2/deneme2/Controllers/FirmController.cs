using TicketSystem.Data;
using TicketSystem.Dtos.Firm;
using TicketSystem.Helpers;
using TicketSystem.Interfaces;
using TicketSystem.Mappers;
using TicketSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace TicketSystem.Controllers
{
     [Route("api/Firm")]
     [ApiController]
     public class FirmController : ControllerBase
     {
          private readonly UserManager<AppUser> _userManager;
          private readonly IFirmRepository _firmRepo;
          public FirmController(ApplicationDbContext context, IFirmRepository firmRepo, UserManager<AppUser> userManager)
          {
               _userManager = userManager;
               _firmRepo = firmRepo;
          }

          [HttpGet("listFirm")]
          public async Task<IActionResult> GetAll()
          {
               if (!ModelState.IsValid)
               {
                    return BadRequest(ModelState);
               }

               var firms = await _firmRepo.GetAllAsync();
               return Ok(firms);
          }

          [HttpGet("listById/{id:int}")]
          public async Task<IActionResult> GetById([FromRoute] int id)
          {
               if (!ModelState.IsValid)
               {
                    return BadRequest(ModelState);
               }
               var firm = await _firmRepo.GetByIdAsync(id);

               if (firm == null)
               {
                    return NotFound();
               }

               return Ok(firm.ToFirmDto());
          }

          [HttpPost("createFirm")]
          public async Task<IActionResult> Create([FromBody] CreateFirmRequestDto firmDto)
          {
               var firmModel = firmDto.ToFirmFromCreateDTO();
               await _firmRepo.CreateAsync(firmModel);

               return CreatedAtAction(nameof(GetById), new { id = firmModel.Id }, firmModel.ToFirmDto());
          }

          [HttpPut("updateFirm/{id:int}")]
          public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateFirmRequestDto updateDto)
          {
               if (!ModelState.IsValid)
               {
                    return BadRequest(ModelState);
               }
               var firmModel = await _firmRepo.UpdateAsync(id, updateDto);

               if (firmModel == null)
               {
                    return NotFound();
               }

               return Ok(firmModel.ToFirmDto());
          }

          [HttpDelete("deleteFirm/{id:int}")]
          public async Task<IActionResult> Delete([FromRoute] int id)
          {
               if (!ModelState.IsValid)
               {
                    return BadRequest(ModelState);
               }
               var firmid = id.ToString();
               var firmModel = await _firmRepo.DeleteAsync(firmid);

               if (firmModel == null)
               {
                    return NotFound();
               }

               return NoContent();
          }
     }
}
