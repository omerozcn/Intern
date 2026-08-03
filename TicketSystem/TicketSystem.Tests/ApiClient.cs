using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TicketSystem.Data;

namespace TicketSystem.Tests;

/// <summary>Sign-in helpers shared by the test classes.</summary>
public static class ApiClient
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static Task<HttpClient> AdminAsync(ApiFactory factory) =>
        SignInAsync(factory, DevelopmentDataSeeder.AdminEmail, DevelopmentDataSeeder.AdminPassword);

    public static Task<HttpClient> UserAsync(ApiFactory factory) =>
        SignInAsync(factory, DevelopmentDataSeeder.UserEmail, DevelopmentDataSeeder.UserPassword);

    public static async Task<HttpClient> SignInAsync(ApiFactory factory, string email, string password)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/account/login", new { email, password });
        response.EnsureSuccessStatusCode();

        var session = await response.Content.ReadFromJsonAsync<AuthSession>(Json);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", session!.AccessToken);
        return client;
    }

    public sealed record AuthSession(string AccessToken);

    public sealed record PagedResponse<T>(
        IReadOnlyList<T> Items,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages,
        bool HasPrevious,
        bool HasNext);

    public sealed record TicketRow(int Id, string Description, string Status, string? Answer);

    public sealed record FirmRow(int Id, string Name, int ProductCount, bool IsProtected);
}
