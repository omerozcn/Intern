using System.Net;
using System.Net.Http.Json;

namespace TicketSystem.Tests;

[Collection(nameof(ApiCollection))]
public sealed class TicketLifecycleTests
{
    private const string ValidDescription =
        "Bu aciklama otuz karakterden uzun oldugu icin dogrulamayi gecmelidir.";

    private readonly ApiFactory _factory;

    public TicketLifecycleTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task User_can_create_edit_and_delete_a_pending_ticket()
    {
        var user = await ApiClient.UserAsync(_factory);

        var created = await CreateTicketAsync(user);
        Assert.Equal("pending", created.Status);

        var edited = await user.PutAsJsonAsync(
            $"/api/tickets/{created.Id}/description",
            new { description = ValidDescription + " Guncellendi." });
        Assert.Equal(HttpStatusCode.OK, edited.StatusCode);

        var deleted = await user.DeleteAsync($"/api/tickets/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
    }

    [Fact]
    public async Task A_ticket_that_left_pending_can_no_longer_be_edited_or_deleted()
    {
        var user = await ApiClient.UserAsync(_factory);
        var admin = await ApiClient.AdminAsync(_factory);
        var ticket = await CreateTicketAsync(user);

        var moved = await admin.PutAsJsonAsync(
            $"/api/tickets/{ticket.Id}/status",
            new { status = "inProgress" });
        Assert.Equal(HttpStatusCode.NoContent, moved.StatusCode);

        var edit = await user.PutAsJsonAsync(
            $"/api/tickets/{ticket.Id}/description",
            new { description = ValidDescription + " Tekrar." });
        Assert.Equal(HttpStatusCode.Conflict, edit.StatusCode);

        var delete = await user.DeleteAsync($"/api/tickets/{ticket.Id}");
        Assert.Equal(HttpStatusCode.Conflict, delete.StatusCode);

        await CleanUpAsync(user, admin, ticket.Id);
    }

    [Fact]
    public async Task Completing_a_ticket_without_an_answer_is_rejected()
    {
        var user = await ApiClient.UserAsync(_factory);
        var admin = await ApiClient.AdminAsync(_factory);
        var ticket = await CreateTicketAsync(user);

        var response = await admin.PutAsJsonAsync(
            $"/api/tickets/{ticket.Id}/status",
            new { status = "completed" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await CleanUpAsync(user, admin, ticket.Id);
    }

    [Theory]
    [InlineData("kisa")]
    [InlineData("             kisa aciklama cok kisa buda             ")]
    public async Task Descriptions_outside_the_allowed_length_are_rejected(string description)
    {
        var user = await ApiClient.UserAsync(_factory);

        var response = await user.PostAsJsonAsync(
            "/api/tickets",
            new { description, newProduct = true });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Creating_against_an_existing_product_requires_a_product_id()
    {
        var user = await ApiClient.UserAsync(_factory);

        var response = await user.PostAsJsonAsync(
            "/api/tickets",
            new { description = ValidDescription, newProduct = false });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task The_owner_and_an_admin_can_both_read_the_ticket()
    {
        var user = await ApiClient.UserAsync(_factory);
        var admin = await ApiClient.AdminAsync(_factory);
        var ticket = await CreateTicketAsync(user);

        Assert.Equal(HttpStatusCode.OK, (await user.GetAsync($"/api/tickets/{ticket.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/tickets/{ticket.Id}")).StatusCode);

        await CleanUpAsync(user, admin, ticket.Id);
    }

    [Fact]
    public async Task Updating_a_ticket_that_does_not_exist_returns_not_found()
    {
        var admin = await ApiClient.AdminAsync(_factory);

        var response = await admin.PutAsJsonAsync(
            "/api/tickets/999999",
            new { status = "inProgress", answer = "x" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<ApiClient.TicketRow> CreateTicketAsync(HttpClient user)
    {
        var response = await user.PostAsJsonAsync(
            "/api/tickets",
            new { description = ValidDescription, newProduct = true });
        response.EnsureSuccessStatusCode();

        var ticket = await response.Content.ReadFromJsonAsync<ApiClient.TicketRow>(ApiClient.Json);
        Assert.NotNull(ticket);
        return ticket!;
    }

    private static async Task CleanUpAsync(HttpClient user, HttpClient admin, int ticketId)
    {
        await admin.PutAsJsonAsync($"/api/tickets/{ticketId}/status", new { status = "pending" });
        await user.DeleteAsync($"/api/tickets/{ticketId}");
    }
}
