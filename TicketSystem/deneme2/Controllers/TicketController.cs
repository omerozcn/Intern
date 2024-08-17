using TicketSystem.Data;
using TicketSystem.Dtos.Ticket;
using TicketSystem.Extensions;
using TicketSystem.Helpers;
using TicketSystem.Interfaces;
using TicketSystem.Mappers;
using TicketSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net.Sockets;

namespace TicketSystem.Controllers
{
     [Route("api/Ticket")]
     [ApiController]
     public class TicketController : ControllerBase
     {
          private readonly UserManager<AppUser> _userManager;
          private readonly ITicketRepository _ticketRepo;
          private readonly IUserTicketRepository _userticketRepo;
          private readonly ILogger<TicketController> _logger;
          public TicketController(ApplicationDbContext context, ITicketRepository ticketRepo, IUserTicketRepository userticketRepo, UserManager<AppUser> userManager, ILogger<TicketController> logger)
          {
               _userManager = userManager;
               _userticketRepo = userticketRepo;
               _ticketRepo = ticketRepo;
               _logger = logger;
          }

          [HttpGet("listTicket")]
          public async Task<IActionResult> GetAll()
          {
               if (!ModelState.IsValid)
               {
                    return BadRequest(ModelState);
               }
               var tickets = await _ticketRepo.GetAllAsync();

               var ticketDto = tickets.Select(t => t.ToString()).ToList();

               return Ok(tickets);
          }

          [HttpGet("listById/{id:int}")]
          public async Task<IActionResult> GetById([FromRoute] int id)
          {
               if (!ModelState.IsValid)
               {
                    return BadRequest(ModelState);
               }
               var ticket = await _ticketRepo.GetByIdAsync(id);

               if (ticket == null)
               {
                    return NotFound();
               }

               return Ok(ticket.ToTicketDto());
          }

          [HttpPost("createTicket")]
          public async Task<IActionResult> Create([FromBody] CreateTicketRequestDto ticketDto)
          {
               var username = User.GetUserName();
               if (username == null)
               {
                    _logger.LogWarning("User is not authenticated.");
                    return Unauthorized();
               }

               var appUser = await _userManager.FindByNameAsync(username);
               if (appUser == null)
               {
                    _logger.LogWarning("Could not find user with username: {UserName}", username);
                    return Unauthorized();
               }

               _logger.LogInformation("Found user: {UserName} with ID: {UserId}", appUser.UserName, appUser.Id);

               var userTicket = await _userticketRepo.GetUserAppUserTicket(appUser);

               var ticketModel = await ticketDto.ToTicketFromCreateDTO();



               await _ticketRepo.CreateAsync(ticketModel);
               var ticketModel2 = new AppUserTicket
               {
                    TicketId = ticketModel.Id,
                    AppUserId = appUser.Id,
               };
               await _ticketRepo.CreateAsyncs(ticketModel2);


               return CreatedAtAction(nameof(GetById), new { id = ticketModel.Id }, ticketModel.ToTicketDto());
          }

          [HttpPut("updateTicket/{id:int}")]
          public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateTicketRequestDto updateDto)
          {
               if (!ModelState.IsValid)
               {
                    return BadRequest(ModelState);
               }
               var ticketModel = await _ticketRepo.UpdateAsync(id, updateDto);

               if (ticketModel == null)
               {
                    return NotFound();
               }

               return Ok(ticketModel.ToTicketDto());
          }

          [HttpDelete("deleteTicket/{id:int}")]
          public async Task<IActionResult> Delete([FromRoute] int id)
          {
               if (!ModelState.IsValid)
               {
                    return BadRequest(ModelState);
               }
               var ticketModel = await _ticketRepo.DeleteAsync(id);

               if (ticketModel == null)
               {
                    return NotFound();
               }

               return NoContent();
          }
     }
}
