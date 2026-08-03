using TicketSystem.Dtos.Ticket;
using TicketSystem.Models;

namespace TicketSystem.Mappers
{
    public static class TicketMappers
    {
        public static TicketDto ToTicketDto(
             this Ticket ticketModel,
             string? firmName = null,
             string? productName = null)
        {
            return new TicketDto
            {
                Id = ticketModel.Id,
                NewProduct = ticketModel.NewProduct,
                Description = ticketModel.Description,
                CreatedBy = ticketModel.CreatedBy ?? string.Empty,
                Status = TicketStatuses.ToApiValue(ticketModel.Status),
                Answer = ticketModel.Answer,
                Updated = ticketModel.Updated,
                Created = ticketModel.Created,
                FirmName = firmName,
                ProductName = productName,
            };
        }

        public static Ticket ToTicketFromCreateDto(
             this CreateTicketRequestDto ticketDto,
             string createdBy)
        {
            return new Ticket
            {
                NewProduct = ticketDto.NewProduct,
                Description = ticketDto.Description.Trim(),
                CreatedBy = createdBy,
                Status = TicketStatuses.Pending,
                Created = DateTime.UtcNow,
                Updated = null,
            };
        }
    }
}
