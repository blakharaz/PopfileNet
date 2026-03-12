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

public class UiPageIntegrationTests : IAsyncLifetime
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
    public async Task SettingsPage_CanSaveSettings()
    {
        var settings = new AppSettings
        {
            ImapSettings = new ImapSettingsDto
            {
                Server = "imap.test.com",
                Port = 993,
                Username = "test@test.com",
                Password = "test",
                UseSsl = true
            }
        };

        var response = await _client!.PostAsJsonAsync("/settings", settings);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SettingsPage_CanTestConnection()
    {
        var response = await _client!.PostAsync("/settings/test-connection", null);

        response.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SyncPage_CanTriggerSync()
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

        response.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task ClassifyPage_CanGetStatus()
    {
        var response = await _client!.GetAsync("/classifier/status");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ClassifyPage_CanTrain()
    {
        var response = await _client!.PostAsync("/classifier/train", null);

        response.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MailsPage_CanViewMails()
    {
        var response = await _client!.GetAsync("/mails");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MailsPage_CanPaginate()
    {
        var response = await _client!.GetAsync("/mails?page=1&pageSize=10");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<PagedApiResponse<EmailDto>>();
        content.ShouldNotBeNull();
        content.Page.ShouldBe(1);
    }

    [Fact]
    public async Task HomePage_CanAccessRoot()
    {
        var response = await _client!.GetAsync("/");

        response.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.Redirect);
    }
}
