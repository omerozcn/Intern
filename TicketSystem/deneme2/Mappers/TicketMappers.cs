using TicketSystem.Dtos.Ticket;
using TicketSystem.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace TicketSystem.Mappers
{
     public static class TicketMappers
     {
          public static TicketDto ToTicketDto(this Ticket ticketModel)
          {
               return new TicketDto
               {
                    Id = ticketModel.Id,
                    Title = ticketModel.Title,
                    Description = ticketModel.Description,
                    CreatedBy = ticketModel.CreatedBy,
               };
          }
          public static async Task<Ticket> ToTicketFromCreateDTO(this CreateTicketRequestDto ticketDto)
          {
               //if (!httpContext.User.Identity.IsAuthenticated)
               //{
               //     throw new ArgumentException("User is not authenticated.");
               //}

               //var userName = JwtHelper.GetUserNameFromToken(httpContext);

               //if (string.IsNullOrEmpty(userName))
               //{
               //     throw new ArgumentException("Authenticated username cannot be null or empty.");
               //}

               //var user = await userManager.FindByNameAsync(userName);

               //if (user == null)
               //{
               //     throw new ArgumentException($"No user found with username: {userName}");
               //}

               //Console.WriteLine($"Authenticated Username: {userName}");

               return new Ticket
               {
                    Title = ticketDto.Title,
                    Description = ticketDto.Description,
                    CreatedBy = ticketDto.CreatedBy
               };

          }
     }


}
