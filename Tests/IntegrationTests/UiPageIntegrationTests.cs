using System.Net;
using System.Net.Http.Json;
using PopfileNet.Backend.Models;
using Shouldly;
using Xunit;

namespace PopfileNet.IntegrationTests;

[Collection("DatabaseTests")]
public class UiPageIntegrationTests : DatabaseTestBase
{
    public UiPageIntegrationTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    protected override Task SetupClientAsync()
    {
        Factory = CreateWebApplicationFactory(Fixture.ConnectionString);
        Client = Factory.CreateClient();
        return Task.CompletedTask;
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

        var response = await Client.PostAsJsonAsync("/settings", settings);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SettingsPage_CanTestConnection()
    {
        var response = await Client.PostAsync("/settings/test-connection", null);

        response.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ClassifyPage_CanGetStatus()
    {
        var response = await Client.GetAsync("/classifier/status");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ClassifyPage_CanTrain()
    {
        var response = await Client.PostAsync("/classifier/train", null);

        response.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MailsPage_CanViewMails()
    {
        var response = await Client.GetAsync("/mails");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MailsPage_CanPaginate()
    {
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

        response.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.Redirect);
    }
}
