using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Models;
using TicketSystem.Security;

namespace TicketSystem.Data;

public static class DevelopmentDataSeeder
{
    public const string AdminEmail = "admin.test@turkuvaz.local";
    public const string AdminPassword = "AdminTest!2026";
    public const string UserEmail = "user.test@turkuvaz.local";
    public const string UserPassword = "UserTest!2026";

    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        try
        {
            await using var scope = services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

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
                context, userManager, AdminEmail, AdminPassword,
                "Test", "Admin", AppRoles.Admin, adminFirm.Id);
            await EnsureUserAsync(
                context, userManager, UserEmail, UserPassword,
                "Test", "Kullanıcı", AppRoles.User, userFirm.Id);

            logger.LogInformation("Development test accounts are ready.");
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Development test accounts could not be seeded.");
        }
    }

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
