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
    public async Task<ActionResult<PagedResult<ProfileDto>>> GetAll(
        [FromQuery] UserListRequest request,
        CancellationToken cancellationToken)
    {
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
        return profile is null ? AccountNotFound() : Ok(profile);
    }

    [HttpPost]
    [ProducesResponseType<ProfileDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProfileDto>> Create(
        [FromBody] RegisterDto registerDto,
        CancellationToken cancellationToken)
    {
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

    private ObjectResult AccountNotFound()
    {
        return Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Account not found");
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
