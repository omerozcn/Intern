using TicketSystem.Interfaces;
using TicketSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Extensions;
using TicketSystem.Repository;

namespace TicketSystem.Controllers
{
     public class FirmUserController : ControllerBase
     {
          private readonly UserManager<AppUser> _userManager;
          private readonly IFirmUserRepository _firmRepository;
          public FirmUserController(UserManager<AppUser> userManager, IFirmUserRepository firmRepository, IUserTicketRepository userticketRepo)
          {
               _userManager = userManager;
               _firmRepository = firmRepository;
          }

          [Route("getuserFirm")]
          [HttpGet]
          public async Task<IActionResult> GetUserFirmUser()
          {
               var username = User.GetUserName();
               var appUser = await _userManager.FindByNameAsync(username);
               var userFirms = await _firmRepository.GetUserFirmUser(appUser);
               return Ok(userFirms);
          }
     }
}
