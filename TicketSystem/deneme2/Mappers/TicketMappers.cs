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
                    NewPorduct = ticketModel.NewProduct,
                    Description = ticketModel.Description,
                    CreatedBy = ticketModel.CreatedBy,
                    Status = ticketModel.Status,
                    Answer = ticketModel.Answer,
                    Updated = ticketModel.Updated,
                    Created = ticketModel.Created,
               };
          }
          public static async Task<Ticket> ToTicketFromCreateDTO(this CreateTicketRequestDto ticketDto)
          {
               return new Ticket
               {
                    NewProduct = ticketDto.NewProduct,
                    Description = ticketDto.Description,
                    CreatedBy = ticketDto.CreatedBy,
                    Status = ticketDto.Status,
               };
          }
     }
}
