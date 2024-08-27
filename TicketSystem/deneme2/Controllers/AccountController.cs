using TicketSystem.Dtos.Account;
using TicketSystem.Models;
using TicketSystem.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Mappers;
using TicketSystem.Data;
using TicketSystem.Dtos.Token;

namespace TicketSystem.Controllers
{
     [Route("api/account")]

     [ApiController]
     public class AccountController : ControllerBase
     {
          private readonly UserManager<AppUser> _userManager;
          private readonly ITokenService _tokenService;
          private readonly SignInManager<AppUser> _signinManager;
          private readonly IAccountRepository _accountRepo;
          private readonly IFirmUserRepository _firmUserRepository;
          private readonly ApplicationDbContext _context;
          public AccountController(UserManager<AppUser> userManager, ITokenService tokenService, SignInManager<AppUser> signInManager, IAccountRepository accountRepo, IFirmRepository firmRepository, IFirmUserRepository firmUserRepository, ApplicationDbContext context)
          {
               _userManager = userManager;
               _tokenService = tokenService;
               _signinManager = signInManager;
               _accountRepo = accountRepo;
               _firmUserRepository = firmUserRepository;
               _context = context;
          }

          [HttpPost("login")]
          public async Task<IActionResult> Login(LoginDto loginDto)
          {
               if (!ModelState.IsValid)
                    return BadRequest(ModelState);

               var user = await _userManager.Users
                         .Include(u => u.FirmUsers)
                         .ThenInclude(cu => cu.Firm)
                         .FirstOrDefaultAsync(x => x.Email == loginDto.Email.ToLower());

               if (user == null) return Unauthorized("Invalid email adress");

               var result = await _signinManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
               var userid = user.Id.ToString();

               if (!result.Succeeded) return Unauthorized("Email adress not found and/or password is incorrect!");
               var firmUser = _context.FirmUsers.Where(fu => fu.AppUserId == userid).Select(firmid => firmid.FirmId).FirstOrDefault();
               if (firmUser == null)
                    return Unauthorized("User is not associated with any firm.");

               var firm = await _context.Firms
                    .Where(f => f.Id == firmUser)
                    .Select(f => f.Name)
                    .FirstOrDefaultAsync();

               var token = await _tokenService.CreateTokenAsync(user);

               await _tokenService.StoreTokenAsync(user, token);



               return Ok(
                   new NewUserDto
                   {
                        UserName = user.UserName,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email,
                        Role = user.Role,
                        FirmId = firmUser,
                        FirmName = firm,
                        Token = token
                   });
          }
          [Authorize]
          [HttpGet("listUsers")]
          public async Task<IActionResult> GetAll()
          {
               if (!ModelState.IsValid)
               {
                    return BadRequest(ModelState);
               }

               var users = await _accountRepo.GetAllAsync();
               return Ok(users);
          }

          [Authorize]
          [HttpGet("listById/{id:Guid}")]
          public async Task<IActionResult> GetById([FromRoute] Guid id)
          {
               if (!ModelState.IsValid)
               {
                    return BadRequest(ModelState);
               }
               var users = await _accountRepo.GetByIdAsync(id);

               if (users == null)
               {
                    return NotFound();
               }

               return Ok(users.ToAccountDto());
          }

          [Authorize]
          [HttpPost("register")]
          public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
          {
               if (!ModelState.IsValid)
                    return BadRequest(ModelState);

               var userName = await _accountRepo.GenerateUniqueUserNameAsync(10);

               var appUser = new AppUser
               {
                    UserName = userName,
                    FirstName = registerDto.FirstName,
                    LastName = registerDto.LastName,
                    Email = registerDto.Email,
                    Role = registerDto.Role,
               };

               var createdUser = await _userManager.CreateAsync(appUser, registerDto.Password);
               await _context.SaveChangesAsync();
               if (!createdUser.Succeeded) return BadRequest(createdUser.Errors);
               var roleResult = await _userManager.AddToRoleAsync(appUser, registerDto.Role);
               var role = await _userManager.GetRolesAsync(appUser);
               if (!roleResult.Succeeded) return BadRequest(roleResult.Errors);


               var firmUser = new FirmUser
               {
                    FirmId = registerDto.FirmId,
                    AppUserId = appUser.Id
               };

               await _firmUserRepository.AddAsync(firmUser);
               await _context.SaveChangesAsync();

               return Ok();
          }

          [Authorize]
          [HttpDelete("deleteUser/{id}")]
          public async Task<IActionResult> Delete([FromRoute] string id)
          {
               if (!ModelState.IsValid)
               {
                    return BadRequest(ModelState);
               }
               try
               {
                    var accountModel = await _accountRepo.DeleteAsync(id);

                    if (accountModel == null)
                    {
                         return NotFound();
                    }

                    return NoContent();
               }
               catch (Exception ex)
               {
                    return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
               }
          }

          [Authorize]
          [HttpPut("updateAccount/{id}")]
          public async Task<IActionResult> Update([FromRoute] string id, [FromBody] UpdateDto updateDto)
          {
               if (!ModelState.IsValid)
               {
                    return BadRequest(ModelState);
               }
               var accountModel = await _accountRepo.UpdateAsync(id, updateDto);

               if (accountModel == null)
               {
                    return NotFound();
               }

               return Ok(accountModel.ToAccountDto());
          }

          [HttpPost("getuserRole")]
          public async Task<IActionResult> GetUserRoleByToken([FromBody] TokenRequestDto request)
          {
               if (!ModelState.IsValid)
               {
                    return BadRequest(ModelState);
               }

               try
               {
                    var userRole = await _accountRepo.GetUserRoleAsync(request.Token);

                    if (userRole == null)
                    {
                         return NotFound("User role not found.");
                    }

                    return Ok(userRole);
               }
               catch (Exception ex)
               {
                    return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
               }
          }

          [Authorize]
          [HttpGet("getbyusername/{username}")]
          public async Task<IActionResult> GetUserByUserNameAsync([FromRoute] string username)
          {
               if (!ModelState.IsValid)
               {
                    return BadRequest(ModelState);
               }

               try
               {
                    var user = await _accountRepo.GetByUserNameAsync(username);

                    if(user == null)
                    {
                         return NotFound("User not found");
                    }

                    return Ok(user);
               }
               catch (Exception ex)
               {
                    return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
               }
          }
     }
}

