using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Models;
using TicketSystem.Security;

namespace TicketSystem.Data;

/// <summary>
/// Creates the two accounts the development environment and the test suite sign in
/// with. Never runs unless <c>Seed:DevelopmentAccounts</c> is switched on.
/// </summary>
public static class DevelopmentDataSeeder
{
    /// <summary>Configuration section that decides whether seeding runs at all.</summary>
    public const string EnabledKey = "Seed:DevelopmentAccounts";

    public const string AdminEmail = "admin.test@turkuvaz.local";
    public const string UserEmail = "user.test@turkuvaz.local";

    // Defaults for local development only. They are published in the README, so any
    // reachable environment must override them or leave seeding switched off.
    private const string DefaultAdminPassword = "AdminTest!2026";
    private const string DefaultUserPassword = "UserTest!2026";

    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // Blank counts as "not supplied": compose passes an empty string for an unset
        // variable, which must not become an empty password.
        var adminPassword = Coalesce(configuration["Seed:AdminPassword"], DefaultAdminPassword);
        var userPassword = Coalesce(configuration["Seed:UserPassword"], DefaultUserPassword);

        try
        {
            await context.Database.MigrateAsync();

            foreach (var role in new[] { AppRoles.Admin, AppRoles.User })
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    EnsureSucceeded(await roleManager.CreateAsync(new IdentityRole(role)));
                }
            }

            var adminFirm = await GetOrCreateFirmAsync(context, ProtectedFirm.Name);
            var userFirm = await GetOrCreateFirmAsync(context, "TEST FIRMASI");

            await EnsureUserAsync(
                context, userManager, AdminEmail, adminPassword,
                "Test", "Admin", AppRoles.Admin, adminFirm.Id);
            await EnsureUserAsync(
                context, userManager, UserEmail, userPassword,
                "Test", "Kullanıcı", AppRoles.User, userFirm.Id);

            logger.LogInformation("Development test accounts are ready.");
        }
        catch (Exception exception)
        {
            // Starting with a half-migrated database only turns into confusing 401s
            // further downstream, so fail here where the cause is still visible.
            logger.LogError(exception, "Development seeding failed; the application cannot start.");
            throw;
        }
    }

    private static string Coalesce(string? configured, string fallback) =>
        string.IsNullOrWhiteSpace(configured) ? fallback : configured;

    private static async Task<Firm> GetOrCreateFirmAsync(
        ApplicationDbContext context,
        string name)
    {
        var firm = await context.Firms.FirstOrDefaultAsync(item => item.Name == name);
        if (firm is not null)
        {
            return firm;
        }

        firm = new Firm { Name = name };
        context.Firms.Add(firm);
        await context.SaveChangesAsync();
        return firm;
    }

    private static async Task EnsureUserAsync(
        ApplicationDbContext context,
        UserManager<AppUser> userManager,
        string email,
        string password,
        string firstName,
        string lastName,
        string role,
        int firmId)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName
            };
            EnsureSucceeded(await userManager.CreateAsync(user, password));
        }
        else
        {
            user.FirstName = firstName;
            user.LastName = lastName;
            user.EmailConfirmed = true;
            EnsureSucceeded(await userManager.UpdateAsync(user));

            if (!await userManager.CheckPasswordAsync(user, password))
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                EnsureSucceeded(await userManager.ResetPasswordAsync(user, token, password));
            }
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        var otherRoles = currentRoles.Where(item => !item.Equals(role, StringComparison.OrdinalIgnoreCase));
        if (otherRoles.Any())
        {
            EnsureSucceeded(await userManager.RemoveFromRolesAsync(user, otherRoles));
        }
        if (!await userManager.IsInRoleAsync(user, role))
        {
            EnsureSucceeded(await userManager.AddToRoleAsync(user, role));
        }

        var firmLinks = await context.FirmUsers.Where(item => item.AppUserId == user.Id).ToListAsync();
        context.FirmUsers.RemoveRange(firmLinks.Where(item => item.FirmId != firmId));
        if (firmLinks.All(item => item.FirmId != firmId))
        {
            context.FirmUsers.Add(new FirmUser { AppUserId = user.Id, FirmId = firmId });
        }
        await context.SaveChangesAsync();
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Description)));
        }
    }
}
