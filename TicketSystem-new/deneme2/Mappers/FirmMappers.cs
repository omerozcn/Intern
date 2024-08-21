using TicketSystem.Dtos.Firm;

using TicketSystem.Models;

namespace TicketSystem.Mappers
{
     public static class FirmMappers
     {
          public static FirmDto ToFirmDto(this Firm firmModel)
          {
               return new FirmDto
               {
                    Id = firmModel.Id,
                    Name = firmModel.Name,
               };
          }
          public static Firm ToFirmFromCreateDTO(this CreateFirmRequestDto firmDto)
          {
               return new Firm
               {
                    Name = firmDto.Name,
               };

          }
     }
}
