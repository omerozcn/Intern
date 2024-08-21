using TicketSystem.Helpers;
using TicketSystem.Models;
using TicketSystem.Dtos.Product;
using TicketSystem.Models.ProductModels;

namespace TicketSystem.Interfaces
{
     public interface IProductRepository
     {
          Task<Product> CreateAsync(Product productModel);
          Task<Product?> DeleteAsync(int id);
          Task<List<ProductSummary>> GetAllAsync();
          Task<Product?> UpdateAsync(int id, UpdateProductRequestDto productDto);
          Task<Product?> GetByNameAsync(string name);
     }
}
