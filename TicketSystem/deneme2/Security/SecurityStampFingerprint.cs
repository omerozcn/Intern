using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace TicketSystem.Security;

internal static class SecurityStampFingerprint
{
    public static string Create(string securityStamp)
    {
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(securityStamp));
        return WebEncoders.Base64UrlEncode(digest);
    }

    public static bool Matches(string fingerprint, string securityStamp)
    {
        try
        {
            var suppliedDigest = WebEncoders.Base64UrlDecode(fingerprint);
            var currentDigest = SHA256.HashData(Encoding.UTF8.GetBytes(securityStamp));

            return suppliedDigest.Length == currentDigest.Length
                && CryptographicOperations.FixedTimeEquals(suppliedDigest, currentDigest);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
