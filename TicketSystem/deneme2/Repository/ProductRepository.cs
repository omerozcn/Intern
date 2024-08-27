using TicketSystem.Data;
using TicketSystem.Dtos.Product;
using TicketSystem.Helpers;
using TicketSystem.Interfaces;
using TicketSystem.Models;
using TicketSystem.Models.ProductModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace TicketSystem.Repository
{
     public class ProductRepository : IProductRepository
     {
          private readonly ApplicationDbContext _context;
          private readonly UserManager<AppUser> _userManager;

          public ProductRepository(ApplicationDbContext context, UserManager<AppUser> userManager)
          {
               _context = context;
               _userManager = userManager;
          }

          public async Task<Product> CreateAsync(Product productsModel)
          {
               await _context.Products.AddAsync(productsModel);
               await _context.SaveChangesAsync();
               return productsModel;
          }

          public async Task<Product?> DeleteAsync(int id)
          {
               var productsModel = await _context.Products.FirstOrDefaultAsync(product => product.Id == id);

               if (productsModel == null)
               {
                    return null;
               }
               _context.Products.Remove(productsModel);

               await _context.SaveChangesAsync();
               return productsModel;
          }

          public async Task<List<ProductSummary>> GetAllAsync()
          {
               var produtcs = await _context.Products
                   .Select(product => new ProductSummary
                   {
                        Id = product.Id,
                        Name = product.Name,
                        BirthDate = product.BirthDate ?? DateTime.MinValue,
                   }).ToListAsync();

               return produtcs;
          }

          public async Task<Product?> GetByIdAsync(int id)
          {
               return await _context.Products.FirstOrDefaultAsync(product => product.Id == id);
          }

          public async Task<Product?> UpdateAsync(int id, UpdateProductRequestDto productDto)
          {
               var existingProduct = await _context.Products.FirstOrDefaultAsync(product => product.Id == id);
               if (existingProduct == null)
               {
                    return null;
               }

               existingProduct.Name = productDto.Name;

               await _context.SaveChangesAsync();
               return existingProduct;
          }
     }
}
