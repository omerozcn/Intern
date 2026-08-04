using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TicketSystem.Configuration;
using TicketSystem.Dtos.Account;
using TicketSystem.Dtos.Token;
using TicketSystem.Interfaces;
using TicketSystem.Models;
using TicketSystem.Security;

namespace TicketSystem.Service;

public sealed class TokenService : ITokenService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly JwtOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly SymmetricSecurityKey _signingKey;

    public TokenService(
        UserManager<AppUser> userManager,
        IOptions<JwtOptions> options,
        TimeProvider timeProvider)
    {
        _userManager = userManager;
        _options = options.Value;
        _timeProvider = timeProvider;
        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
    }

    public async Task<IssuedTokenDto> CreateTokenAsync(
        AppUser user,
        ProfileDto profile,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (profile.Firm is null)
        {
            throw new InvalidOperationException("The account must be associated with a firm before a token can be issued.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Count == 0)
        {
            throw new InvalidOperationException("The account must have an Identity role before a token can be issued.");
        }

        var securityStamp = await _userManager.GetSecurityStampAsync(user);
        if (string.IsNullOrWhiteSpace(securityStamp))
        {
            var stampResult = await _userManager.UpdateSecurityStampAsync(user);
            if (!stampResult.Succeeded)
            {
                throw new InvalidOperationException("A security stamp could not be created for the account.");
            }

            securityStamp = await _userManager.GetSecurityStampAsync(user);
        }

        var displayName = string.Join(
            ' ',
            new[] { profile.FirstName, profile.LastName }
                .Where(value => !string.IsNullOrWhiteSpace(value)));
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = profile.UserName;
        }

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(AuthClaimTypes.UserId, user.Id),
            new(AuthClaimTypes.Name, displayName),
            new(AuthClaimTypes.UserName, profile.UserName),
            new(AuthClaimTypes.Email, profile.Email),
            new(AuthClaimTypes.FirmId, profile.Firm.Id.ToString(CultureInfo.InvariantCulture)),
            new(AuthClaimTypes.FirmName, profile.Firm.Name ?? string.Empty),
            new(AuthClaimTypes.SecurityStamp, SecurityStampFingerprint.Create(securityStamp)),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        claims.AddRange(
            roles
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(role => role, StringComparer.OrdinalIgnoreCase)
                .Select(role => new Claim(AuthClaimTypes.Role, role)));

        var issuedAt = _timeProvider.GetUtcNow();
        var expiresAt = issuedAt.AddMinutes(_options.ExpirationMinutes);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: issuedAt.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256));

        return new IssuedTokenDto(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
