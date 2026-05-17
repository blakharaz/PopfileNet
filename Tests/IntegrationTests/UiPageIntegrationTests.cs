using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using PopfileNet.Backend.Models;
using PopfileNet.Common;
using Shouldly;
using Xunit;

namespace PopfileNet.IntegrationTests;

[Collection("DatabaseTests")]
public class UiPageIntegrationTests : DatabaseTestBase
{
    private const string AdminEmail = "test@popfile.local";
    private const string AdminPassword = "testpassword123";

    public UiPageIntegrationTests(DatabaseFixture fixture) : base(fixture)
    {
    }

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
    public async Task SettingsPage_CanSaveSettings()
    {
        await LoginAsync();

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

        var response = await Client.PostAsJsonAsync("/settings", settings);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SettingsPage_CanTestConnection()
    {
        await LoginAsync();

        var response = await Client.PostAsync("/settings/test-connection", null);

        response.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ClassifyPage_CanGetStatus()
    {
        await LoginAsync();

        var response = await Client.GetAsync("/classifier/status");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ClassifyPage_CanTrain()
    {
        await LoginAsync();

        var response = await Client.PostAsync("/classifier/train", null);

        response.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MailsPage_CanViewMails()
    {
        await LoginAsync();

        var response = await Client.GetAsync("/mails");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MailsPage_CanPaginate()
    {
        await LoginAsync();

        var response = await Client.GetAsync("/mails?page=1&pageSize=10");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<PagedApiResponse<EmailDto>>();
        content.ShouldNotBeNull();
        content.Page.ShouldBe(1);
    }

    [Fact]
    public async Task HomePage_CanAccessRoot()
    {
        var response = await Client.GetAsync("/");

        response.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.Redirect, HttpStatusCode.Found);
    }
}
