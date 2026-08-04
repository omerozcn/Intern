using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Data;
using TicketSystem.Dtos.Account;
using TicketSystem.Dtos.Common;
using TicketSystem.Dtos.Firm;
using TicketSystem.Extensions;
using TicketSystem.Interfaces;
using TicketSystem.Models;
using TicketSystem.Security;

namespace TicketSystem.Repositories;

public sealed class AccountRepository : IAccountRepository
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AccountRepository(
        ApplicationDbContext context,
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<PagedResult<ProfileDto>> GetAllAsync(
        UserListRequest request,
        CancellationToken cancellationToken = default)
    {
        // Filters run against the entity, before the projection: EF cannot translate a
        // predicate applied to the subqueries that build ProfileDto.
        var users = _context.Users.AsNoTracking();

        if (request.FirmId.HasValue)
        {
            users = users.Where(user =>
                user.FirmUsers.Any(firmUser => firmUser.FirmId == request.FirmId.Value));
        }

        if (request.Role is not null)
        {
            users = users.Where(user =>
                _context.UserRoles
                    .Where(userRole => userRole.UserId == user.Id)
                    .Join(_context.Roles, userRole => userRole.RoleId, role => role.Id, (_, role) => role.Name)
                    .Any(name => name == request.Role));
        }

        if (request.Search is not null)
        {
            users = users.Where(user =>
                (user.FirstName != null && user.FirstName.Contains(request.Search)) ||
                user.LastName.Contains(request.Search) ||
                (user.Email != null && user.Email.Contains(request.Search)));
        }

        return await Profiles(users)
            .OrderBy(user => user.FirstName)
            .ThenBy(user => user.LastName)
            .ToPagedResultAsync(request, cancellationToken);
    }

    public Task<ProfileDto?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        return Profiles().SingleOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public async Task<ProfileDto?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = _userManager.NormalizeEmail(email.Trim());
        var userId = await _context.Users
            .AsNoTracking()
            .Where(account => account.NormalizedEmail == normalizedEmail)
            .Select(account => account.Id)
            .SingleOrDefaultAsync(cancellationToken);

        return userId is null
            ? null
            : await GetByIdAsync(userId, cancellationToken);
    }

    public async Task<IdentityResult> CreateAsync(
        RegisterDto registerDto,
        CancellationToken cancellationToken = default)
    {
        var role = await ResolveRoleAsync(registerDto.Role);
        if (role is null)
        {
            return Invalid("InvalidRole", "Role must be either Admin or User.");
        }

        var firmName = await _context.Firms
            .Where(firm => firm.Id == registerDto.FirmId)
            .Select(firm => firm.Name)
            .SingleOrDefaultAsync(cancellationToken);
        if (firmName is null)
        {
            return Invalid("InvalidFirm", "The selected firm does not exist.");
        }

        if (role == AppRoles.User && ProtectedFirm.IsProtectedName(firmName))
        {
            return ProtectedFirmRejected();
        }

        var user = new AppUser
        {
            UserName = $"u{Guid.NewGuid():N}",
            FirstName = registerDto.FirstName.Trim(),
            LastName = registerDto.LastName.Trim(),
            Email = registerDto.Email.Trim().ToLowerInvariant()
        };

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var createResult = await _userManager.CreateAsync(user, registerDto.Password);
            if (!createResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return createResult;
            }

            var roleResult = await _userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return roleResult;
            }

            _context.FirmUsers.Add(new FirmUser
            {
                AppUserId = user.Id,
                FirmId = registerDto.FirmId
            });
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return IdentityResult.Success;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IdentityResult?> UpdateAsync(
        string id,
        UpdateUserRequestDto updateDto,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .Include(account => account.FirmUsers)
            .SingleOrDefaultAsync(account => account.Id == id, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var role = await ResolveRoleAsync(updateDto.Role);
        if (role is null)
        {
            return Invalid("InvalidRole", "Role must be either Admin or User.");
        }

        var firmName = await _context.Firms
            .Where(firm => firm.Id == updateDto.FirmId)
            .Select(firm => firm.Name)
            .SingleOrDefaultAsync(cancellationToken);
        if (firmName is null)
        {
            return Invalid("InvalidFirm", "The selected firm does not exist.");
        }

        if (role == AppRoles.User && ProtectedFirm.IsProtectedName(firmName))
        {
            return ProtectedFirmRejected();
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            user.FirstName = updateDto.FirstName.Trim();
            user.LastName = updateDto.LastName.Trim();
            user.Email = updateDto.Email.Trim().ToLowerInvariant();

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return updateResult;
            }

            var existingRoles = await _userManager.GetRolesAsync(user);
            if (existingRoles.Count != 1
                || !string.Equals(existingRoles[0], role, StringComparison.OrdinalIgnoreCase))
            {
                if (existingRoles.Count > 0)
                {
                    var removeResult = await _userManager.RemoveFromRolesAsync(user, existingRoles);
                    if (!removeResult.Succeeded)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return removeResult;
                    }
                }

                var addResult = await _userManager.AddToRoleAsync(user, role);
                if (!addResult.Succeeded)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return addResult;
                }
            }

            var primaryFirm = user.FirmUsers.OrderBy(firmUser => firmUser.Id).FirstOrDefault();
            if (primaryFirm is null)
            {
                _context.FirmUsers.Add(new FirmUser
                {
                    AppUserId = user.Id,
                    FirmId = updateDto.FirmId
                });
            }
            else
            {
                primaryFirm.FirmId = updateDto.FirmId;
                _context.FirmUsers.RemoveRange(user.FirmUsers.Where(firmUser => firmUser.Id != primaryFirm.Id));
            }

            await _context.SaveChangesAsync(cancellationToken);

            var stampResult = await _userManager.UpdateSecurityStampAsync(user);
            if (!stampResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return stampResult;
            }

            await transaction.CommitAsync(cancellationToken);
            return IdentityResult.Success;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IdentityResult?> DeleteAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return null;
        }

        if (await _context.AppUserTickets.AnyAsync(
                assignment => assignment.AppUserId == id,
                cancellationToken))
        {
            return Invalid(
                "AccountHasTicketHistory",
                "Accounts referenced by ticket history cannot be deleted.");
        }

        return await _userManager.DeleteAsync(user);
    }

    private IQueryable<ProfileDto> Profiles(IQueryable<AppUser>? source = null)
    {
        return (source ?? _context.Users.AsNoTracking())
            .Select(user => new ProfileDto
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                // The Identity role tables are the only source of truth for a user's role.
                Role = (
                    from userRole in _context.UserRoles
                    join identityRole in _context.Roles on userRole.RoleId equals identityRole.Id
                    where userRole.UserId == user.Id
                    orderby identityRole.Name
                    select identityRole.Name).FirstOrDefault() ?? string.Empty,
                Firm = user.FirmUsers
                    .OrderBy(firmUser => firmUser.Id)
                    .Select(firmUser => new FirmDto
                    {
                        Id = firmUser.FirmId,
                        Name = firmUser.Firm.Name
                    })
                    .FirstOrDefault()
            });
    }

    private async Task<string?> ResolveRoleAsync(string requestedRole)
    {
        var canonicalRole = requestedRole.Trim() switch
        {
            var value when value.Equals(AppRoles.Admin, StringComparison.OrdinalIgnoreCase) => AppRoles.Admin,
            var value when value.Equals(AppRoles.User, StringComparison.OrdinalIgnoreCase) => AppRoles.User,
            _ => null
        };

        return canonicalRole is not null && await _roleManager.RoleExistsAsync(canonicalRole)
            ? canonicalRole
            : null;
    }

    private static IdentityResult ProtectedFirmRejected()
    {
        return Invalid(
            "InvalidFirm",
            $"User accounts cannot be assigned to the protected {ProtectedFirm.Name} firm.");
    }

    private static IdentityResult Invalid(string code, string description)
    {
        return IdentityResult.Failed(new IdentityError
        {
            Code = code,
            Description = description
        });
    }
}
