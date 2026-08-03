using System.Net;
using System.Net.Http.Json;

namespace TicketSystem.Tests;

[Collection(nameof(ApiCollection))]
public sealed class PaginationTests
{
    private readonly ApiFactory _factory;

    public PaginationTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task List_endpoints_return_the_paged_envelope()
    {
        var admin = await ApiClient.AdminAsync(_factory);

        var page = await admin.GetFromJsonAsync<ApiClient.PagedResponse<ApiClient.FirmRow>>(
            "/api/Firm/listFirm",
            ApiClient.Json);

        Assert.NotNull(page);
        Assert.Equal(1, page!.Page);
        Assert.Equal(20, page.PageSize);
        Assert.True(page.TotalCount >= 2, "the seeder creates at least two firms");
        Assert.Equal(page.TotalCount, page.Items.Count);
    }

    [Fact]
    public async Task Page_size_is_clamped_to_the_maximum()
    {
        var admin = await ApiClient.AdminAsync(_factory);

        var page = await admin.GetFromJsonAsync<ApiClient.PagedResponse<ApiClient.FirmRow>>(
            "/api/Firm/listFirm?pageSize=100000",
            ApiClient.Json);

        Assert.Equal(100, page!.PageSize);
    }

    [Fact]
    public async Task Out_of_range_paging_values_fall_back_to_the_defaults()
    {
        var admin = await ApiClient.AdminAsync(_factory);

        var page = await admin.GetFromJsonAsync<ApiClient.PagedResponse<ApiClient.FirmRow>>(
            "/api/Firm/listFirm?page=-5&pageSize=0",
            ApiClient.Json);

        Assert.Equal(1, page!.Page);
        Assert.Equal(20, page.PageSize);
    }

    [Fact]
    public async Task Paging_walks_the_whole_result_set_without_repeats()
    {
        var admin = await ApiClient.AdminAsync(_factory);

        var first = await admin.GetFromJsonAsync<ApiClient.PagedResponse<ApiClient.FirmRow>>(
            "/api/Firm/listFirm?page=1&pageSize=1",
            ApiClient.Json);
        var second = await admin.GetFromJsonAsync<ApiClient.PagedResponse<ApiClient.FirmRow>>(
            "/api/Firm/listFirm?page=2&pageSize=1",
            ApiClient.Json);

        Assert.Single(first!.Items);
        Assert.Single(second!.Items);
        Assert.NotEqual(first.Items[0].Id, second.Items[0].Id);
        Assert.True(first.HasNext);
        Assert.True(second.HasPrevious);
    }

    [Fact]
    public async Task Search_narrows_the_result_set()
    {
        var admin = await ApiClient.AdminAsync(_factory);

        var matches = await admin.GetFromJsonAsync<ApiClient.PagedResponse<ApiClient.FirmRow>>(
            "/api/Firm/listFirm?search=TURKUVAZ",
            ApiClient.Json);
        var noMatches = await admin.GetFromJsonAsync<ApiClient.PagedResponse<ApiClient.FirmRow>>(
            "/api/Firm/listFirm?search=bulunmayan-firma-adi",
            ApiClient.Json);

        Assert.Single(matches!.Items);
        Assert.True(matches.Items[0].IsProtected);
        Assert.Empty(noMatches!.Items);
    }

    [Fact]
    public async Task Status_filtering_happens_server_side()
    {
        var admin = await ApiClient.AdminAsync(_factory);

        var completed = await admin.GetFromJsonAsync<ApiClient.PagedResponse<ApiClient.TicketRow>>(
            "/api/Ticket/listTicket?status=completed",
            ApiClient.Json);

        Assert.NotNull(completed);
        Assert.All(completed!.Items, ticket => Assert.Equal("completed", ticket.Status));
    }

    [Fact]
    public async Task Role_filtering_happens_server_side()
    {
        var admin = await ApiClient.AdminAsync(_factory);

        var admins = await admin.GetFromJsonAsync<ApiClient.PagedResponse<ProfileRow>>(
            "/api/account/listUsers?role=Admin",
            ApiClient.Json);

        Assert.NotNull(admins);
        Assert.NotEmpty(admins!.Items);
        Assert.All(admins.Items, profile => Assert.Equal("Admin", profile.Role));
    }

    [Fact]
    public async Task Duplicate_firm_names_are_rejected_with_a_conflict()
    {
        var admin = await ApiClient.AdminAsync(_factory);
        var name = $"Benzersiz Firma {Guid.NewGuid():N}";

        var created = await admin.PostAsJsonAsync("/api/Firm/createFirm", new { name });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var duplicate = await admin.PostAsJsonAsync("/api/Firm/createFirm", new { name });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var firm = await created.Content.ReadFromJsonAsync<ApiClient.FirmRow>(ApiClient.Json);
        await admin.DeleteAsync($"/api/Firm/deleteFirm/{firm!.Id}");
    }

    [Fact]
    public async Task The_protected_firm_cannot_be_created_or_deleted()
    {
        var admin = await ApiClient.AdminAsync(_factory);

        var create = await admin.PostAsJsonAsync("/api/Firm/createFirm", new { name = "turkuvaz" });
        Assert.Equal(HttpStatusCode.Conflict, create.StatusCode);

        var firms = await admin.GetFromJsonAsync<ApiClient.PagedResponse<ApiClient.FirmRow>>(
            "/api/Firm/listFirm?search=TURKUVAZ",
            ApiClient.Json);
        var protectedFirm = firms!.Items.Single();

        var delete = await admin.DeleteAsync($"/api/Firm/deleteFirm/{protectedFirm.Id}");
        Assert.Equal(HttpStatusCode.Conflict, delete.StatusCode);
    }

    private sealed record ProfileRow(string Id, string Email, string Role);
}
