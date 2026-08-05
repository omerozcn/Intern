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

/// <summary>
/// Authorization policies layered on top of the two roles.
///
/// There is no third Identity role: a super administrator is simply an administrator
/// whose firm is <see cref="ProtectedFirm.Name"/>, which is the firm that owns the
/// platform. Deriving it keeps the role table at two entries and needs no migration,
/// and the firm is already carried in the token as a claim.
/// </summary>
public static class AppPolicies
{
    /// <summary>
    /// Administrators of the platform owner. They may create and remove administrator
    /// accounts and manage the firm and service catalogues.
    /// </summary>
    public const string SuperAdmin = "super-admin";
}

/// <summary>
/// The firm that owns the platform. It is seeded by the initial migration and must never be
/// renamed, deleted, or assigned to a non-admin account.
/// </summary>
public static class ProtectedFirm
{
    public const string Name = "TURKUVAZ";

    public static bool IsProtectedName(string? name) =>
        string.Equals(name?.Trim(), Name, StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Fixed identifiers for the roles seeded by the initial migration. They must stay constant:
/// letting <see cref="Microsoft.AspNetCore.Identity.IdentityRole"/> generate them makes every
/// model build produce different values, which EF then reports as a pending schema change.
/// </summary>
public static class SeededRoleIds
{
    public const string Admin = "7b2cb566-795e-4d3b-9be0-7bacd6d773d7";
    public const string User = "7d41b27d-2be6-40b5-b8cc-6eebb987c378";
}
