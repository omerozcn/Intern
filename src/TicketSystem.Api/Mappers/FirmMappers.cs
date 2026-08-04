using TicketSystem.Dtos.Firm;
using TicketSystem.Models;
using TicketSystem.Security;

namespace TicketSystem.Mappers;

public static class FirmMappers
{
    public static FirmDto ToFirmDto(this Firm firmModel)
    {
        return new FirmDto
        {
            Id = firmModel.Id,
            Name = firmModel.Name,
            IsProtected = ProtectedFirm.IsProtectedName(firmModel.Name),
        };
    }

    public static Firm ToFirmFromCreateDTO(this CreateFirmRequestDto firmDto)
    {
        // [Required] on the DTO means model validation rejects a null name before this runs.
        return new Firm
        {
            Name = firmDto.Name ?? string.Empty,
        };
    }
}
