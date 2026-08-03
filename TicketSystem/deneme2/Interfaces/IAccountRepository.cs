using Microsoft.AspNetCore.Identity;
using TicketSystem.Dtos.Account;

namespace TicketSystem.Interfaces;

public interface IAccountRepository
{
    Task<IReadOnlyList<ProfileDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProfileDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<ProfileDto?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IdentityResult> CreateAsync(RegisterDto registerDto, CancellationToken cancellationToken = default);
    Task<IdentityResult?> UpdateAsync(string id, UpdateDto updateDto, CancellationToken cancellationToken = default);
    Task<IdentityResult?> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
