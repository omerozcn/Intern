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
               return new Ticket
               {
                    Title = ticketDto.Title,
                    Description = ticketDto.Description,
                    CreatedBy = ticketDto.CreatedBy,
                    Status = 1
               };

          }
     }


}
