using System.Net;
using Shouldly;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using PopfileNet.Backend;
using Reqnroll;

namespace PopfileNet.IntegrationTests;

[Binding]
public class BackendApiSteps
{
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;
    private HttpResponseMessage? _response;

    [Given("the API is running")]
    public void GivenTheApiIsRunning()
    {
        Environment.SetEnvironmentVariable("SKIP_DB_INIT", "true");
        
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ImapSettings:Server"] = "imap.test.com",
                        ["ImapSettings:Port"] = "993",
                        ["ImapSettings:Username"] = "test@test.com",
                        ["ImapSettings:Password"] = "test",
                        ["ImapSettings:UseSsl"] = "true"
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
        _response!.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [AfterScenario]
    public void Cleanup()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }
}
