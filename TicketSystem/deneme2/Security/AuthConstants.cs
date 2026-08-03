namespace TicketSystem.Security;

public static class AuthClaimTypes
{
    public const string UserId = "id";
    public const string Name = "name";
    public const string UserName = "username";
    public const string Email = "email";
    public const string Role = "role";
    public const string FirmId = "firmId";
    public const string FirmName = "firmName";
    public const string SecurityStamp = "security_stamp";
}

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string User = "User";

    /// <summary>
    /// Both roles, in the comma separated form <see cref="Microsoft.AspNetCore.Authorization.AuthorizeAttribute"/> expects.
    /// </summary>
    public const string AdminOrUser = Admin + "," + User;
}

public static class AuthRateLimitPolicies
{
    public const string Login = "auth-login";
    public const string Password = "auth-password";
}
