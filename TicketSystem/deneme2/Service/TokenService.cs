using TicketSystem.Models;
using TicketSystem.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace TicketSystem.Service
{
     public class TokenService : ITokenService
     {
          private readonly UserManager<AppUser> _userManager;
          private readonly IConfiguration _config;
          private readonly SymmetricSecurityKey _key;
          public TokenService(UserManager<AppUser> userManager, IConfiguration config)
          {
               _userManager = userManager;
               _config = config;
               _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT:SigningKey"]));
          }
          public async Task<string> CreateTokenAsync(AppUser user)
          {
               var roles = await _userManager.GetRolesAsync(user);

               var claims = new List<Claim>
               {
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim(JwtRegisteredClaimNames.GivenName, user.UserName),
                    new Claim(JwtRegisteredClaimNames.Name, user.FirstName)
               };

               foreach (var role in roles)
               {
                    claims.Add(new Claim(ClaimTypes.Role, role));
               }

               var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha512Signature);

               var tokenDescriptor = new SecurityTokenDescriptor
               {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.Now.AddDays(7),
                    SigningCredentials = creds,
                    Issuer = _config["JWT:Issuer"],
                    Audience = _config["JWT:Audience"]
               };

               var tokenHandler = new JwtSecurityTokenHandler();
               var token = tokenHandler.CreateToken(tokenDescriptor);

               return tokenHandler.WriteToken(token);
          }

          public async Task StoreTokenAsync(AppUser user, string token)
          {
               const string loginProvider = "MyAppJwt";
               const string tokenName = "JWT";

               var existingToken = await _userManager.GetAuthenticationTokenAsync(user, loginProvider, tokenName);
               if (!string.IsNullOrEmpty(existingToken))
               {

                    await _userManager.RemoveAuthenticationTokenAsync(user, loginProvider, tokenName);
               }

               await _userManager.SetAuthenticationTokenAsync(user, loginProvider, tokenName, token);
          }
     }
}
