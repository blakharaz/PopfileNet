using System.Net;
using System.Net.Http.Json;
using PopfileNet.Backend.Models;
using Shouldly;
using Xunit;

namespace PopfileNet.IntegrationTests;

[Collection("Database")]
public class MailsApiTests(DatabaseFixture fixture) : DatabaseTestBase(fixture)
{
    protected override Task SetupClientAsync()
    {
        var factory = CreateWebApplicationFactory(Fixture.ConnectionString);
        Client = factory.CreateClient();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetMails_ReturnsPagedResults()
    {
        var response = await Client.GetAsync("/mails");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<PagedApiResponse<EmailDto>>();
        content.ShouldNotBeNull();
        content.IsSuccess.ShouldBeTrue();
        content.Items.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetMails_WithPagination_ReturnsCorrectPage()
    {
        var response = await Client.GetAsync("/mails?page=1&pageSize=10");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<PagedApiResponse<EmailDto>>();
        content.ShouldNotBeNull();
        content.IsSuccess.ShouldBeTrue();
        content.Page.ShouldBe(1);
        content.PageSize.ShouldBe(10);
    }

    [Fact]
    public async Task GetMailById_NotFound_Returns404()
    {
        var response = await Client.GetAsync("/mails/non-existent-id");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<EmailDetailDto>>();
        content.ShouldNotBeNull();
        content.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task GetFolders_ReturnsPagedResults()
    {
        var response = await Client.GetAsync("/folders");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<PagedApiResponse<FolderDto>>();
        content.ShouldNotBeNull();
        content.IsSuccess.ShouldBeTrue();
        content.Items.ShouldNotBeNull();
    }
}
