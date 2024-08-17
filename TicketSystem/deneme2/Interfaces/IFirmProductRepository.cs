using TicketSystem.Dtos.FirmProduct;
using TicketSystem.Models;
using TicketSystem.Models.FirmProductModels;

namespace TicketSystem.Interfaces
{
     public interface IFirmProductRepository
     {
          Task<FirmProduct> CreateAsync(CreateFirmProductRequestDto firmproductDto);
          Task<List<FirmProductSummary>> GetAllAsyncs();
          Task<FirmProduct> DeleteAsyncs(int id);
     }
}
