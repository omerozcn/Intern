using Microsoft.AspNetCore.Identity;
using TicketSystem.Dtos.Account;
using TicketSystem.Dtos.Common;

namespace TicketSystem.Interfaces;

public interface IAccountRepository
{
    Task<PagedResult<ProfileDto>> GetAllAsync(UserListRequest request, CancellationToken cancellationToken = default);
    Task<ProfileDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<ProfileDto?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IdentityResult> CreateAsync(RegisterDto registerDto, CancellationToken cancellationToken = default);
    Task<IdentityResult?> UpdateAsync(string id, UpdateUserRequestDto updateDto, CancellationToken cancellationToken = default);
    Task<IdentityResult?> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
