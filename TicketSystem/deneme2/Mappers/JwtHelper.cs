using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace TicketSystem.Mappers
{
     public static class JwtHelper
     {
          public static string GetUserNameFromToken(HttpContext httpContext)
          {
               if (httpContext == null || httpContext.User == null)
               {
                    return null;
               }

               var userNameClaim = httpContext.User.FindFirst(JwtRegisteredClaimNames.GivenName);
               return userNameClaim?.Value;
          }
     }
}
