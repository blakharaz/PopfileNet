using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using PopfileNet.Backend;
using PopfileNet.Backend.Models;
using Shouldly;
using Testcontainers.PostgreSql;
using Xunit;

namespace PopfileNet.IntegrationTests;

public class MailsApiTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder(image: "postgres:16-alpine")
        .WithDatabase("popfilenet")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private HttpClient? _client;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        
        var connectionString = _postgres.GetConnectionString();
        
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:popfilenet"] = connectionString,
                        ["ImapSettings:Server"] = "imap.test.com",
                        ["ImapSettings:Port"] = "993",
                        ["ImapSettings:Username"] = "test@test.com",
                        ["ImapSettings:Password"] = "test",
                        ["ImapSettings:UseSsl"] = "true"
                    });
                });
            });

        _client = factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task GetMails_ReturnsPagedResults()
    {
        var response = await _client!.GetAsync("/mails");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<PagedApiResponse<EmailDto>>();
        content.ShouldNotBeNull();
        content.IsSuccess.ShouldBeTrue();
        content.Items.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetMails_WithPagination_ReturnsCorrectPage()
    {
        var response = await _client!.GetAsync("/mails?page=1&pageSize=10");

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
        var response = await _client!.GetAsync("/mails/non-existent-id");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<EmailDetailDto>>();
        content.ShouldNotBeNull();
        content.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task GetFolders_ReturnsPagedResults()
    {
        var response = await _client!.GetAsync("/folders");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<PagedApiResponse<FolderDto>>();
        content.ShouldNotBeNull();
        content.IsSuccess.ShouldBeTrue();
        content.Items.ShouldNotBeNull();
    }

    [Fact]
    public async Task Sync_WithoutImapConfiguration_Returns500()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:popfilenet"] = _postgres.GetConnectionString()
                    });
                });
            });

        using var client = factory.CreateClient();
        var response = await client.PostAsync("/jobs/sync", null);

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<SyncJobResult>>();
        content.ShouldNotBeNull();
        content.IsSuccess.ShouldBeFalse();
    }
}
