using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using PopfileNet.Backend.Models;
using Shouldly;
using Xunit;

namespace PopfileNet.IntegrationTests;

[Collection("DatabaseTests")]
public class MailsApiTests(DatabaseFixture fixture) : DatabaseTestBase(fixture)
{
    private const string AdminEmail = "test@popfile.local";
    private const string AdminPassword = "testpassword123";

    protected override Task SetupClientAsync()
    {
        Factory = CreateWebApplicationFactory(Fixture.ConnectionString);
        Client = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        return Task.CompletedTask;
    }


    private async Task LoginAsync()
    {
        var loginRequest = new LoginRequest(AdminEmail, AdminPassword);
        var response = await Client.PostAsJsonAsync("/auth/login", loginRequest);
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetMails_ReturnsPagedResults()
    {
        await LoginAsync();
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
        await LoginAsync();
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
        await LoginAsync();
        var response = await Client.GetAsync("/mails/non-existent-id");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<EmailDetailDto>>();
        content.ShouldNotBeNull();
        content.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task GetFolders_ReturnsPagedResults()
    {
        await LoginAsync();
        var response = await Client.GetAsync("/folders");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<PagedApiResponse<FolderDto>>();
        content.ShouldNotBeNull();
        content.IsSuccess.ShouldBeTrue();
        content.Items.ShouldNotBeNull();
    }
}
