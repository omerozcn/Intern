using TicketSystem.Dtos.Product;
using TicketSystem.Models;

namespace TicketSystem.Mappers;

public static class ProductMappers
{
    public static ProductDto ToProductDto(this Product productModel)
    {
        return new ProductDto
        {
            Id = productModel.Id,
            Name = productModel.Name,
            BirthDate = productModel.BirthDate,
        };
    }

    public static Product ToProductFromCreateDTO(this CreateProductRequestDto productDto)
    {
        // [Required] on the DTO means model validation rejects a null name before this runs.
        return new Product
        {
            Name = productDto.Name ?? string.Empty,
        };
    }
}
