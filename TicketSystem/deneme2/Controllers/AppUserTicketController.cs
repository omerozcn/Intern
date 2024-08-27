using TicketSystem.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Models;
using Microsoft.AspNetCore.Authorization;
using TicketSystem.Extensions;

namespace TicketSystem.Controllers
{
     [Route("api/userTicket")]
     [Authorize]
     public class AppUserTicketController : ControllerBase
     {
          private readonly UserManager<AppUser> _userManager;
          private readonly ITicketRepository _ticketRepo;
          private readonly IUserTicketRepository _userticketRepo;
          public AppUserTicketController(UserManager<AppUser> userManager,
              ITicketRepository ticketRepo, IUserTicketRepository userticketRepo)
          {
               _userManager = userManager;
               _ticketRepo = ticketRepo;
               _userticketRepo = userticketRepo;
          }

          [Route("getuserTicket")]
          [HttpGet]
          public async Task<IActionResult> GetUserAppUserTicket()
          {
               var username = User.GetUserName();
               var appUser = await _userManager.FindByNameAsync(username);
               var userTickets = await _userticketRepo.GetUserAppUserTicket(appUser);
               return Ok(userTickets);
          }
          [Route("addticketToUser")]
          [HttpPost]
          public async Task<IActionResult> AddUserTickets(int id)
          {
               var username = User.GetUserName();
               var appUser = await _userManager.FindByNameAsync(username);
               var ticket = await _ticketRepo.GetByIdAsync(id);

               if (ticket == null) return BadRequest("Ticket not found");

               var ticketModel = new AppUserTicket
               {
                    TicketId = ticket.Id,
                    AppUserId = appUser.Id,
               };

               await _userticketRepo.CreateAsync(ticketModel);

               if (ticketModel == null)
               {
                    return StatusCode(500, "Could not create");
               }
               else
               {
                    return Created();
               }
          }
          // düzelt
          //[HttpDelete]
          //public async Task<IActionResult> DeleteUserTickets(string title)
          //{
          //     var username = User.GetUserName();
          //     var appUser = await _userManager.FindByNameAsync(username);

          //     var userTickets = await _userticketRepo.GetUserAppUserTicket(appUser);

          //     var filteredTicket = userTickets.Where(t => t.Title.ToLower() == title.ToLower()).ToList();

          //     if (filteredTicket.Count() == 1)
          //     {
          //          await _userticketRepo.DeleteAppUserTicket(appUser, title);
          //     }
          //     else
          //     {
          //          return BadRequest("Ticket is not in your AppUserTickets");
          //     }

          //     return Ok();
          //}
     }
}
