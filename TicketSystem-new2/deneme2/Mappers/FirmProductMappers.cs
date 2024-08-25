using TicketSystem.Dtos.Feedback;
using TicketSystem.Dtos.FirmProduct;
using TicketSystem.Models;

namespace TicketSystem.Mappers
{
     public static class FirmProductMappers
     {
          public static FirmProductDto ToFirmProductDto(this FirmProduct firmproductModel)
          {
               return new FirmProductDto
               {
                    Id = firmproductModel.Id,
                    FirmId = firmproductModel.FirmId,
                    ProductId = firmproductModel.ProductId,
               };
          }
          public static FirmProduct ToFirmProductFromCreateDTO(this CreateFirmProductRequestDto firmproductDto)
          {
               return new FirmProduct
               {
                    FirmId = firmproductDto.FirmId,
                    ProductId = firmproductDto.ProductId,
               };

          }
     }
}
