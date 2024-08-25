using System.Security.Claims;

namespace TicketSystem.Extensions
{
     public static class ClaimsExtensions
     {
          public static string GetUserName(this ClaimsPrincipal user)
          {
               return user.Claims.SingleOrDefault(user => user.Type.Equals("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname")).Value;
          }
     }
}
