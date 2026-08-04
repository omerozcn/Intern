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
using TicketSystem.Infrastructure;
using TicketSystem.Extensions;
using TicketSystem.Interfaces;
using TicketSystem.Models;
using TicketSystem.Security;

namespace TicketSystem.Controllers;

/// <summary>Sign-in, sign-out and password lifecycle for the current session.</summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private const string GenericResetMessage =
        "If an account exists for that email address, a password reset link has been sent.";

    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly IAccountRepository _accountRepository;
    private readonly IPasswordResetNotifier _emailQueue;
    private readonly FrontendOptions _frontendOptions;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        ITokenService tokenService,
        IAccountRepository accountRepository,
        IPasswordResetNotifier emailQueue,
        IOptions<FrontendOptions> frontendOptions,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _accountRepository = accountRepository;
        _emailQueue = emailQueue;
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
        // Authentication outcomes are logged with the account id and the client address
        // only. The submitted email and password never reach the log.
        var clientAddress = HttpContext.Connection.RemoteIpAddress;

        var user = await _userManager.FindByEmailAsync(loginDto.Email.Trim());
        if (user is null)
        {
            _logger.LogWarning(
                "Sign-in failed for an unknown address from {ClientAddress}.", clientAddress);
            return AuthenticationFailed();
        }

        var signInResult = await _signInManager.CheckPasswordSignInAsync(
            user,
            loginDto.Password,
            lockoutOnFailure: true);
        if (!signInResult.Succeeded)
        {
            if (signInResult.IsLockedOut)
            {
                _logger.LogWarning(
                    "Sign-in rejected: account {AccountId} is locked out. Client {ClientAddress}.",
                    user.Id,
                    clientAddress);
            }
            else
            {
                _logger.LogWarning(
                    "Sign-in failed for account {AccountId} from {ClientAddress}.",
                    user.Id,
                    clientAddress);
            }

            return AuthenticationFailed();
        }

        var profile = await _accountRepository.GetByIdAsync(user.Id, cancellationToken);
        if (profile?.Firm is null || string.IsNullOrWhiteSpace(profile.Role))
        {
            _logger.LogWarning(
                "Account {AccountId} signed in but has no role or firm assigned.", user.Id);
            return Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Account is not configured",
                detail: "Ask an administrator to assign this account to a role and firm.");
        }

        _logger.LogInformation(
            "Account {AccountId} signed in from {ClientAddress}.", user.Id, clientAddress);

        var issuedToken = await _tokenService.CreateTokenAsync(user, profile, cancellationToken);
        return Ok(new AuthSessionDto
        {
            AccessToken = issuedToken.AccessToken,
            ExpiresAt = issuedToken.ExpiresAt,
            User = profile
        });
    }

    /// <summary>The profile of the signed-in account.</summary>
    [Authorize]
    [HttpGet("me")]
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

            // Queued rather than sent inline: an inline SMTP round trip would make the
            // response measurably slower for a known address than an unknown one, which
            // is an account-enumeration oracle regardless of the identical body below.
            _emailQueue.Enqueue(new PasswordResetEmail(
                user.Email,
                string.IsNullOrWhiteSpace(recipientName) ? user.Email : recipientName,
                resetUrl));

            _logger.LogInformation("Password reset requested for account {AccountId}.", user.Id);
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
                : this.IdentityFailure(resetResult);
        }

        var stampResult = await _userManager.UpdateSecurityStampAsync(user);
        if (!stampResult.Succeeded)
        {
            throw new InvalidOperationException("The account security stamp could not be refreshed.");
        }

        // No cancellation check past this point: the password has already changed, so
        // throwing here would tell the caller it failed when it did not.
        _logger.LogInformation("Password reset completed for account {AccountId}.", user.Id);
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
            return this.IdentityFailure(changeResult);
        }

        var stampResult = await _userManager.UpdateSecurityStampAsync(user);
        if (!stampResult.Succeeded)
        {
            throw new InvalidOperationException("The account security stamp could not be refreshed.");
        }

        // See ResetPassword: the change is already committed.
        _logger.LogInformation("Password changed for account {AccountId}.", user.Id);
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

        // Bumping the stamp invalidates every token already issued for this account.
        var result = await _userManager.UpdateSecurityStampAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException("The account security stamp could not be refreshed.");
        }

        // See ResetPassword: the tokens are already revoked.
        _logger.LogInformation("Account {AccountId} signed out.", user.Id);
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

    private string BuildResetUrl(string email, string encodedToken)
    {
        var baseUri = new Uri(_frontendOptions.BaseUrl.TrimEnd('/') + '/', UriKind.Absolute);
        var resetUri = new Uri(baseUri, "reset-password").ToString();

        // Carried in the fragment, not the query string: a fragment is never sent to a
        // server, so the token stays out of proxy logs and out of the Referer header of
        // anything the reset page loads.
        var fragment = QueryHelpers.AddQueryString(
            string.Empty,
            new Dictionary<string, string?>
            {
                ["email"] = email,
                ["token"] = encodedToken
            });

        return $"{resetUri}#{fragment.TrimStart('?')}";
    }
}
