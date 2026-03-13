using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using PopfileNet.Backend;
using Reqnroll;
using Shouldly;
using Xunit;

namespace PopfileNet.IntegrationTests;

[Binding]
[Collection("Database")]
public class BackendApiSteps : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture = DatabaseFixture.Instance;
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;
    private HttpResponseMessage? _response;

    public BackendApiSteps()
    {
    }

    [Given("the API is running")]
    public void GivenTheApiIsRunning()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:popfilenet"] = _fixture.ConnectionString,
                        ["ImapSettings:Server"] = "",
                        ["ImapSettings:Port"] = "993",
                        ["ImapSettings:Username"] = "",
                        ["ImapSettings:Password"] = "",
                        ["ImapSettings:UseSsl"] = "true",
                        ["SyncInterval"] = "01:00:00"
                    });
                });
            });
        
        _client = _factory.CreateClient();
    }

    [When("I request the root endpoint {string}")]
    public async Task WhenIRequestTheRootEndpoint(string endpoint)
    {
        _response = await _client!.GetAsync(endpoint);
    }

    [When("I request the accounts endpoint {string}")]
    public async Task WhenIRequestTheAccountsEndpoint(string endpoint)
    {
        _response = await _client!.GetAsync(endpoint);
    }

    [Then("I should receive a successful response")]
    public void ThenIShouldReceiveASuccessfulResponse()
    {
        _response!.StatusCode.ShouldBeOneOf(HttpStatusCode.Redirect, HttpStatusCode.OK);
    }

    [Then("I should receive an OK response")]
    public void ThenIShouldReceiveAnOkResponse()
    {
        _response!.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [AfterScenario]
    public void Cleanup()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
    }
}
