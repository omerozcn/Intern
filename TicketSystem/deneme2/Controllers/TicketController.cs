using TicketSystem.Data;
using TicketSystem.Dtos.Ticket;
using TicketSystem.Extensions;
using TicketSystem.Interfaces;
using TicketSystem.Mappers;
using TicketSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TicketSystem.Controllers
{
     [Route("api/Ticket")]
     [Authorize]
     [ApiController]
     public class TicketController : ControllerBase
     {
          private readonly UserManager<AppUser> _userManager;
          private readonly ITicketRepository _ticketRepo;
          private readonly IUserTicketRepository _userticketRepo;
          private readonly ApplicationDbContext _context;
          private readonly ILogger<TicketController> _logger;
          private readonly IProductRepository _productRepo;
          public TicketController(ApplicationDbContext context, ITicketRepository ticketRepo, IUserTicketRepository userticketRepo, UserManager<AppUser> userManager, ILogger<TicketController> logger, IProductRepository productRepo)
          {
               _userManager = userManager;
               _userticketRepo = userticketRepo;
               _ticketRepo = ticketRepo;
               _logger = logger;
               _productRepo = productRepo;
               _context = context;
          }

          [HttpGet("listTicket")]
          public async Task<IActionResult> GetAll()
          {
               if (!ModelState.IsValid)
               {
                    return BadRequest(ModelState);
               }
               var tickets = await _ticketRepo.GetAllAsync();

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

          [HttpGet("listByUserId")]
          public async Task<IActionResult> GetByUserId()
          {
               if (!ModelState.IsValid)
               {
                    return BadRequest(ModelState);
               }
               var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

               var userToken = await _context.UserTokens
                    .FirstOrDefaultAsync(ut => ut.LoginProvider == "MyAppJwt" && ut.Name == "JWT" && ut.Value == token);

               var ticket = await _ticketRepo.GetByUserIdAsync(userToken.UserId);

               var ticketDtos = ticket.ToList();

               return Ok(ticketDtos);
          }

          [HttpPost("createTicket")]
          [Authorize]
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


               var ticketModel = await ticketDto.ToTicketFromCreateDTO();

               await _ticketRepo.CreateAsync(ticketModel);
               var ticketModel2 = new AppUserTicket
               {
                    TicketId = ticketModel.Id,
                    AppUserId = appUser.Id,
               };
               await _ticketRepo.AppUserTicketCreateAsync(ticketModel2);

               var product = await _productRepo.GetByIdAsync(ticketDto.ProductId);

               if (ticketDto.NewProduct == false)
               {
                    var ticketModel3 = new ProductTicket
                    {
                         TicketId = ticketModel.Id,
                         ProductId = product.Id

                    };
                    await _ticketRepo.ProductTicketCreateAsync(ticketModel3);
               }


               return CreatedAtAction(nameof(GetById), new { id = ticketModel.Id }, ticketModel.ToTicketDto());
          }

          [HttpPut("updateTicket/{id}")]
          public async Task<IActionResult> Update([FromRoute] int id, UpdateTicketRequestDto updateDto)
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

          [HttpDelete("deleteTicket/{id}")]
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

          [HttpPut("updateStatus/{id}")]
          public async Task<IActionResult> UpdateTicketStatus(int id, [FromBody] UpdateStatusTicketDto updateTicketStatusDto)
          {
               var ticket = await _ticketRepo.GetByIdAsync(id);
               if (ticket == null)
               {
                    return NotFound();
               }

               ticket.Status = updateTicketStatusDto.Status;
               await _ticketRepo.UpdateTicketStatusAsync(ticket);

               return NoContent();
          }
     }
}
