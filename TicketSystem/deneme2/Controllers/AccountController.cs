using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using TicketSystem.Configuration;
using TicketSystem.Dtos.Account;
using TicketSystem.Interfaces;
using TicketSystem.Models;
using TicketSystem.Security;

namespace TicketSystem.Controllers;

[ApiController]
[Route("api/account")]
public sealed class AccountController : ControllerBase
{
    private const string GenericResetMessage =
        "If an account exists for that email address, a password reset link has been sent.";

    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly IAccountRepository _accountRepository;
    private readonly IEmailService _emailService;
    private readonly FrontendOptions _frontendOptions;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        ITokenService tokenService,
        IAccountRepository accountRepository,
        IEmailService emailService,
        IOptions<FrontendOptions> frontendOptions,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _accountRepository = accountRepository;
        _emailService = emailService;
        _frontendOptions = frontendOptions.Value;
        _logger = logger;
    }

    [AllowAnonymous]
    [EnableRateLimiting(AuthRateLimitPolicies.Login)]
    [HttpPost("login")]
    [ProducesResponseType<AuthSessionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthSessionDto>> Login(
        [FromBody] LoginDto loginDto,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(loginDto.Email.Trim());
        if (user is null)
        {
            return AuthenticationFailed();
        }

        var signInResult = await _signInManager.CheckPasswordSignInAsync(
            user,
            loginDto.Password,
            lockoutOnFailure: true);
        if (!signInResult.Succeeded)
        {
            return AuthenticationFailed();
        }

        var profile = await _accountRepository.GetByIdAsync(user.Id, cancellationToken);
        if (profile?.Firm is null || string.IsNullOrWhiteSpace(profile.Role))
        {
            return Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Account is not configured",
                detail: "Ask an administrator to assign this account to a role and firm.");
        }

        var issuedToken = await _tokenService.CreateTokenAsync(user, profile, cancellationToken);
        return Ok(new AuthSessionDto
        {
            AccessToken = issuedToken.AccessToken,
            ExpiresAt = issuedToken.ExpiresAt,
            User = profile
        });
    }

    [Authorize]
    [HttpGet("me")]
    [HttpGet("getbyusername")]
    [ProducesResponseType<ProfileDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProfileDto>> Me(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(AuthClaimTypes.UserId);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var profile = await _accountRepository.GetByIdAsync(userId, cancellationToken);
        return profile is null ? Unauthorized() : Ok(profile);
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpGet("listUsers")]
    [ProducesResponseType<IReadOnlyList<ProfileDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProfileDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        return Ok(await _accountRepository.GetAllAsync(cancellationToken));
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpGet("listById/{id}")]
    [ProducesResponseType<ProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfileDto>> GetById(
        [FromRoute] string id,
        CancellationToken cancellationToken)
    {
        var profile = await _accountRepository.GetByIdAsync(id, cancellationToken);
        return profile is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Account not found")
            : Ok(profile);
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost("register")]
    [ProducesResponseType<ProfileDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ProfileDto>> Register(
        [FromBody] RegisterDto registerDto,
        CancellationToken cancellationToken)
    {
        var result = await _accountRepository.CreateAsync(registerDto, cancellationToken);
        if (!result.Succeeded)
        {
            return IdentityFailure(result);
        }

        var profile = await _accountRepository.GetByEmailAsync(registerDto.Email, cancellationToken);
        if (profile is null)
        {
            throw new InvalidOperationException("The new account could not be reloaded.");
        }

        return CreatedAtAction(nameof(GetById), new { id = profile.Id }, profile);
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPut("updateAccount/{id}")]
    [ProducesResponseType<ProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfileDto>> Update(
        [FromRoute] string id,
        [FromBody] UpdateDto updateDto,
        CancellationToken cancellationToken)
    {
        var result = await _accountRepository.UpdateAsync(id, updateDto, cancellationToken);
        if (result is null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Account not found");
        }

        if (!result.Succeeded)
        {
            return IdentityFailure(result);
        }

        var profile = await _accountRepository.GetByIdAsync(id, cancellationToken);
        if (profile is null)
        {
            throw new InvalidOperationException("The updated account could not be reloaded.");
        }

        return Ok(profile);
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpDelete("deleteUser/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromRoute] string id,
        CancellationToken cancellationToken)
    {
        var result = await _accountRepository.DeleteAsync(id, cancellationToken);
        if (result is null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Account not found");
        }

        if (result.Errors.Any(error => error.Code == "AccountHasTicketHistory"))
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Account cannot be deleted",
                detail: "Accounts referenced by ticket history must be retained.");
        }

        return result.Succeeded ? NoContent() : IdentityFailure(result);
    }

    [AllowAnonymous]
    [EnableRateLimiting(AuthRateLimitPolicies.Password)]
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordDto forgotPasswordDto,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(forgotPasswordDto.Email.Trim());
        if (user is not null && !string.IsNullOrWhiteSpace(user.Email))
        {
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(resetToken));
            var resetUrl = BuildResetUrl(user.Email, encodedToken);
            var recipientName = string.Join(
                ' ',
                new[] { user.FirstName, user.LastName }
                    .Where(value => !string.IsNullOrWhiteSpace(value)));

            try
            {
                await _emailService.SendPasswordResetAsync(
                    user.Email,
                    string.IsNullOrWhiteSpace(recipientName) ? user.Email : recipientName,
                    resetUrl,
                    cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogError(
                    exception,
                    "Password reset email delivery failed for account {AccountId}",
                    user.Id);
            }
        }

        return Accepted(new { message = GenericResetMessage });
    }

    [AllowAnonymous]
    [EnableRateLimiting(AuthRateLimitPolicies.Password)]
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordDto resetPasswordDto,
        CancellationToken cancellationToken)
    {
        string resetToken;
        try
        {
            resetToken = Encoding.UTF8.GetString(
                WebEncoders.Base64UrlDecode(resetPasswordDto.Token.Trim()));
        }
        catch (FormatException)
        {
            return InvalidResetRequest();
        }

        var user = await _userManager.FindByEmailAsync(resetPasswordDto.Email.Trim());
        if (user is null)
        {
            return InvalidResetRequest();
        }

        var resetResult = await _userManager.ResetPasswordAsync(
            user,
            resetToken,
            resetPasswordDto.NewPassword);
        if (!resetResult.Succeeded)
        {
            return resetResult.Errors.Any(error => error.Code == "InvalidToken")
                ? InvalidResetRequest()
                : IdentityFailure(resetResult);
        }

        var stampResult = await _userManager.UpdateSecurityStampAsync(user);
        if (!stampResult.Succeeded)
        {
            throw new InvalidOperationException("The account security stamp could not be refreshed.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        return NoContent();
    }

    [Authorize]
    [EnableRateLimiting(AuthRateLimitPolicies.Password)]
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordDto changePasswordDto,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(AuthClaimTypes.UserId);
        var user = string.IsNullOrWhiteSpace(userId)
            ? null
            : await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Unauthorized();
        }

        var changeResult = await _userManager.ChangePasswordAsync(
            user,
            changePasswordDto.CurrentPassword,
            changePasswordDto.NewPassword);
        if (!changeResult.Succeeded)
        {
            return IdentityFailure(changeResult);
        }

        var stampResult = await _userManager.UpdateSecurityStampAsync(user);
        if (!stampResult.Succeeded)
        {
            throw new InvalidOperationException("The account security stamp could not be refreshed.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        return NoContent();
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(AuthClaimTypes.UserId);
        var user = string.IsNullOrWhiteSpace(userId)
            ? null
            : await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Unauthorized();
        }

        var result = await _userManager.UpdateSecurityStampAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException("The account security stamp could not be refreshed.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        return NoContent();
    }

    private ObjectResult AuthenticationFailed()
    {
        return Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Authentication failed",
            detail: "Invalid email or password.");
    }

    private ObjectResult InvalidResetRequest()
    {
        return Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Invalid password reset request",
            detail: "The password reset link is invalid or has expired.");
    }

    private ObjectResult IdentityFailure(IdentityResult result)
    {
        var errors = result.Errors
            .GroupBy(error => string.IsNullOrWhiteSpace(error.Code) ? "account" : error.Code)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Description).Distinct().ToArray());

        var problem = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Account validation failed",
            Instance = HttpContext.Request.Path
        };
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;

        var response = BadRequest(problem);
        response.ContentTypes.Add("application/problem+json");
        return response;
    }

    private string BuildResetUrl(string email, string encodedToken)
    {
        var baseUri = new Uri(_frontendOptions.BaseUrl.TrimEnd('/') + '/', UriKind.Absolute);
        var resetUri = new Uri(baseUri, "reset-password").ToString();
        return QueryHelpers.AddQueryString(
            resetUri,
            new Dictionary<string, string?>
            {
                ["email"] = email,
                ["token"] = encodedToken
            });
    }
}
