using System.Net;
using System.Net.Http.Json;

namespace TicketSystem.Tests;

[Collection(nameof(ApiCollection))]
public sealed class AuthorizationTests
{
    private readonly ApiFactory _factory;

    public AuthorizationTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/api/Ticket/listTicket")]
    [InlineData("/api/Product/listProduct")]
    [InlineData("/api/Firm/listFirm")]
    [InlineData("/api/account/listUsers")]
    [InlineData("/api/Feedback/listFeedbacks")]
    public async Task Anonymous_requests_are_rejected(string path)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/Ticket/listTicket")]
    [InlineData("/api/Product/listProduct")]
    [InlineData("/api/Firm/listFirm")]
    [InlineData("/api/account/listUsers")]
    [InlineData("/api/Feedback/listFeedbacks")]
    public async Task Admin_only_endpoints_reject_the_user_role(string path)
    {
        var client = await ApiClient.UserAsync(_factory);

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/Ticket/listByUserId")]
    [InlineData("/api/Firmproduct/listProductsForCurrentUser")]
    public async Task User_only_endpoints_reject_the_admin_role(string path)
    {
        var client = await ApiClient.AdminAsync(_factory);

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_credentials_return_unauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/account/login",
            new { email = "admin.test@turkuvaz.local", password = "wrong-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Profile_reports_the_role_from_the_identity_tables()
    {
        var client = await ApiClient.AdminAsync(_factory);

        var profile = await client.GetFromJsonAsync<ProfileResponse>("/api/account/me", ApiClient.Json);

        Assert.NotNull(profile);
        Assert.Equal("Admin", profile!.Role);
        Assert.NotNull(profile.Firm);
    }

    private sealed record ProfileResponse(string Id, string Email, string Role, FirmRef? Firm);

    private sealed record FirmRef(int Id, string Name);
}
