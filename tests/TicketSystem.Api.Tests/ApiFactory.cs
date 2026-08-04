using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TicketSystem.Data;

namespace TicketSystem.Tests;

/// <summary>
/// Boots the real API against a throwaway database.
///
/// The connection comes from TEST_DB_CONNECTION so the same tests run against LocalDB on a
/// developer machine and against the SQL Server service container in CI.
///
/// A real SQL Server is required, but not because of the isolation level — the in-memory
/// provider rejects every transaction, serializable or not. The actual blockers are that
/// startup calls MigrateAsync, that UniqueConstraintExceptionHandler matches SqlException
/// error numbers, and that the search endpoints rely on a case-insensitive collation.
/// SQLite in-memory would satisfy the first two but not the third.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>Seed password this test host installs; not a shipped credential.</summary>
    public const string AdminPassword = "IntegrationAdmin!2026";

    /// <inheritdoc cref="AdminPassword"/>
    public const string UserPassword = "IntegrationUser!2026";

    private const string DefaultConnection =
        "Server=(localdb)\\MSSQLLocalDB;Database={0};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

    private readonly string _databaseName = $"TicketSystemTests_{Guid.NewGuid():N}";

    private string ConnectionString
    {
        get
        {
            var template = Environment.GetEnvironmentVariable("TEST_DB_CONNECTION");
            return string.IsNullOrWhiteSpace(template)
                ? string.Format(DefaultConnection, _databaseName)
                : template.Replace("{DATABASE}", _databaseName, StringComparison.Ordinal);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // Seeding is opt-in; it applies migrations and creates the two accounts the
        // tests sign in with. The passwords are supplied here rather than taken from
        // the API's defaults, so the tests do not depend on a shipped credential.
        builder.UseSetting("Seed:DevelopmentAccounts", "true");
        builder.UseSetting("Seed:AdminPassword", AdminPassword);
        builder.UseSetting("Seed:UserPassword", UserPassword);

        builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
        builder.UseSetting("Jwt:Issuer", "http://localhost");
        builder.UseSetting("Jwt:Audience", "http://localhost");
        builder.UseSetting("Jwt:SigningKey", "integration-tests-signing-key-at-least-32-bytes-long");
        builder.UseSetting("Frontend:BaseUrl", "http://localhost:5173");
        builder.UseSetting("Cors:AllowedOrigins:0", "http://localhost:5173");

        // Rate limits would make a test run flaky, so raise them well above what tests need.
        builder.UseSetting("RateLimiting:GlobalPermitLimit", "100000");
        builder.UseSetting("RateLimiting:LoginPermitLimit", "100000");
        builder.UseSetting("RateLimiting:PasswordPermitLimit", "100000");
    }

    public Task InitializeAsync()
    {
        // Touching the client triggers host startup, which migrates and seeds the database.
        _ = CreateClient();
        return Task.CompletedTask;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        using (var scope = Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.EnsureDeletedAsync();
        }

        await base.DisposeAsync();
    }
}

[CollectionDefinition(nameof(ApiCollection))]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>;
