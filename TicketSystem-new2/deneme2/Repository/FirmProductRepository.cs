using TicketSystem.Data;
using TicketSystem.Dtos.FirmProduct;
using TicketSystem.Interfaces;
using TicketSystem.Models;
using TicketSystem.Models.FirmProductModels;
using TicketSystem.Models.TicketModels;
using Microsoft.EntityFrameworkCore;

namespace TicketSystem.Repository
{
     public class FirmProductRepository : IFirmProductRepository
     {
          private ApplicationDbContext _context;
          public FirmProductRepository(ApplicationDbContext context)
          {
               _context = context;
          }

          public async Task<FirmProduct> CreateAsync(CreateFirmProductRequestDto firmproductDto)
          {
               try
               {
                    var firmExists = await _context.Firms.AnyAsync(f => f.Id == firmproductDto.FirmId);
                    if (!firmExists)
                    {
                         throw new Exception("FirmId does not exist.");
                    }

                    var existingFirmProduct = await _context.FirmProducts
            .AnyAsync(fp => fp.FirmId == firmproductDto.FirmId && fp.ProductId == firmproductDto.ProductId);

                    var productExists = await _context.Products.AnyAsync(p => p.Id == firmproductDto.ProductId);
                    if (!productExists)
                    {
                         throw new Exception("ProductId does not exist.");
                    }

                    var firmproductModel = new FirmProduct
                    {
                         FirmId = firmproductDto.FirmId,
                         ProductId = firmproductDto.ProductId
                    };

                    await _context.FirmProducts.AddAsync(firmproductModel);
                    await _context.SaveChangesAsync();

                    return firmproductModel;
               }
               catch (Exception ex)
               {
                    throw new Exception("An error occurred while creating the FirmProduct.", ex);
               }
          }

          public async Task<FirmProduct> DeleteAsyncs(int id)
          {
               // Log the ID for debugging purposes
               Console.WriteLine($"Attempting to delete FirmProduct with Id: {id}");

               var firmproductModel = await _context.FirmProducts
                   .FirstOrDefaultAsync(x => x.Id == id);

               if (firmproductModel == null)
               {
                    // Log if the entity is not found
                    Console.WriteLine($"FirmProduct with Id: {id} not found.");
                    return null;
               }

               _context.FirmProducts.Remove(firmproductModel);
               await _context.SaveChangesAsync();

               // Log the successful deletion
               Console.WriteLine($"FirmProduct with Id: {id} successfully deleted.");
               return firmproductModel;
          }

          public async Task<List<FirmProductSummary>> GetAllAsyncs()
          {
               var firmproductModel = await _context.FirmProducts
                   .Select(t => new FirmProductSummary
                   {
                        Id = t.Id,
                        FirmId = t.FirmId,
                        FirmName = t.Firm.Name,
                        ProductId = t.ProductId,
                        ProductName = t.Products.Name
                   }).ToListAsync();

               foreach (var item in firmproductModel)
               {
                    Console.WriteLine($"Id: {item.Id}, FirmId: {item.FirmId}, FirmName: {item.FirmName}, ProductId: {item.ProductId}, ProductName: {item.ProductName}");
               }

               return firmproductModel;
          }
     }
}
