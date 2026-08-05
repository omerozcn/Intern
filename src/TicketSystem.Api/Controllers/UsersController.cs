using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Dtos.Account;
using TicketSystem.Dtos.Common;
using TicketSystem.Extensions;
using TicketSystem.Interfaces;
using TicketSystem.Security;

namespace TicketSystem.Controllers;

/// <summary>Administrative management of the accounts other people sign in with.</summary>
[ApiController]
[Authorize(Roles = AppRoles.Admin)]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly IAccountRepository _accountRepository;

    public UsersController(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    [HttpGet]
    [ProducesResponseType<PagedResult<ProfileDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<ProfileDto>>> GetAll(
        [FromQuery] UserListRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsSuperAdmin)
        {
            if (CallerFirmId is not { } firmId)
            {
                return NoFirmScope();
            }

            // Overwritten, not validated: a firm filter supplied by the caller must never
            // widen the scope, so the claim always wins over the query string.
            request.FirmId = firmId;
        }

        return Ok(await _accountRepository.GetAllAsync(request, cancellationToken));
    }

    [HttpGet("{id}")]
    [ProducesResponseType<ProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfileDto>> GetById(
        [FromRoute] string id,
        CancellationToken cancellationToken)
    {
        var profile = await _accountRepository.GetByIdAsync(id, cancellationToken);
        if (profile is null || !CanReach(profile))
        {
            // 404 rather than 403 for an account in another firm: a 403 would confirm
            // the id exists, which is exactly what the firm boundary is hiding.
            return AccountNotFound();
        }

        return Ok(profile);
    }

    [HttpPost]
    [ProducesResponseType<ProfileDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProfileDto>> Create(
        [FromBody] RegisterDto registerDto,
        CancellationToken cancellationToken)
    {
        var wantsAdmin = string.Equals(registerDto.Role, AppRoles.Admin, StringComparison.OrdinalIgnoreCase);

        if (!IsSuperAdmin)
        {
            if (wantsAdmin)
            {
                return AdminManagementRejected();
            }

            if (CallerFirmId is not { } firmId)
            {
                return NoFirmScope();
            }

            // Same reasoning as the list filter: the caller does not get to choose the
            // firm, so an account can never be created outside their own.
            registerDto.FirmId = firmId;
        }

        var result = await _accountRepository.CreateAsync(registerDto, cancellationToken);
        if (!result.Succeeded)
        {
            return this.IdentityFailure(result);
        }

        var profile = await _accountRepository.GetByEmailAsync(registerDto.Email, cancellationToken);
        if (profile is null)
        {
            throw new InvalidOperationException("The new account could not be reloaded.");
        }

        return CreatedAtAction(nameof(GetById), new { id = profile.Id }, profile);
    }

    [HttpPut("{id}")]
    [ProducesResponseType<ProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfileDto>> Update(
        [FromRoute] string id,
        [FromBody] UpdateUserRequestDto updateDto,
        CancellationToken cancellationToken)
    {
        // Changing your own role revokes your own token mid-request and can leave the
        // system with no administrator at all.
        if (SelfTargeted(id) && !string.Equals(updateDto.Role, AppRoles.Admin, StringComparison.Ordinal))
        {
            return SelfModificationRejected(
                "Administrators cannot change their own role. Ask another administrator.");
        }

        if (!IsSuperAdmin)
        {
            if (CallerFirmId is not { } firmId)
            {
                return NoFirmScope();
            }

            var target = await _accountRepository.GetByIdAsync(id, cancellationToken);
            if (target is null || !CanReach(target))
            {
                return AccountNotFound();
            }

            // Both directions are closed: an existing administrator cannot be edited, and
            // a plain account cannot be promoted into one.
            if (IsAdmin(target.Role) ||
                string.Equals(updateDto.Role, AppRoles.Admin, StringComparison.OrdinalIgnoreCase))
            {
                return AdminManagementRejected();
            }

            updateDto.FirmId = firmId;
        }

        var result = await _accountRepository.UpdateAsync(id, updateDto, cancellationToken);
        if (result is null)
        {
            return AccountNotFound();
        }

        if (!result.Succeeded)
        {
            return this.IdentityFailure(result);
        }

        var profile = await _accountRepository.GetByIdAsync(id, cancellationToken);
        if (profile is null)
        {
            throw new InvalidOperationException("The updated account could not be reloaded.");
        }

        return Ok(profile);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        [FromRoute] string id,
        CancellationToken cancellationToken)
    {
        if (SelfTargeted(id))
        {
            return SelfModificationRejected(
                "Administrators cannot delete their own account. Ask another administrator.");
        }

        if (!IsSuperAdmin)
        {
            if (CallerFirmId is null)
            {
                return NoFirmScope();
            }

            var target = await _accountRepository.GetByIdAsync(id, cancellationToken);
            if (target is null || !CanReach(target))
            {
                return AccountNotFound();
            }

            if (IsAdmin(target.Role))
            {
                return AdminManagementRejected();
            }
        }

        var result = await _accountRepository.DeleteAsync(id, cancellationToken);
        if (result is null)
        {
            return AccountNotFound();
        }

        if (result.Errors.Any(error => error.Code == "AccountHasTicketHistory"))
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Account cannot be deleted",
                detail: "Accounts referenced by ticket history must be retained.");
        }

        return result.Succeeded ? NoContent() : this.IdentityFailure(result);
    }

    /// <summary>
    /// Administrators of the platform owner manage every account. Every other
    /// administrator is scoped to their own firm and cannot touch administrator accounts
    /// at all, in either direction.
    /// </summary>
    private bool IsSuperAdmin => User.IsSuperAdmin();

    private int? CallerFirmId => User.GetFirmId();

    private static bool IsAdmin(string? role) =>
        string.Equals(role, AppRoles.Admin, StringComparison.OrdinalIgnoreCase);

    private bool CanReach(ProfileDto profile) =>
        IsSuperAdmin || (profile.Firm is not null && profile.Firm.Id == CallerFirmId);

    private ObjectResult AccountNotFound()
    {
        return Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Account not found");
    }

    private ObjectResult AdminManagementRejected()
    {
        return Problem(
            statusCode: StatusCodes.Status403Forbidden,
            title: "Administrator accounts are restricted",
            detail: $"Only administrators of {ProtectedFirm.Name} can create or manage administrator accounts.");
    }

    /// <summary>
    /// An administrator with no firm has no scope to work within, so the safe answer is
    /// none rather than everything.
    /// </summary>
    private ObjectResult NoFirmScope()
    {
        return Problem(
            statusCode: StatusCodes.Status403Forbidden,
            title: "Account has no firm",
            detail: "This administrator account is not linked to a firm and cannot manage accounts.");
    }

    private bool SelfTargeted(string id) =>
        string.Equals(User.GetUserId(), id, StringComparison.Ordinal);

    private ObjectResult SelfModificationRejected(string detail)
    {
        return Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Account cannot modify itself",
            detail: detail);
    }
}
