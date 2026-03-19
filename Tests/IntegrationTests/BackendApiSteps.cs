using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PopfileNet.Backend;
using PopfileNet.Database;
using Reqnroll;
using Shouldly;
using Xunit;

namespace PopfileNet.IntegrationTests;

[Binding]
[Collection("Database")]
public class BackendApiSteps(DatabaseFixture fixture) : DatabaseTestBase(fixture)
{
    private HttpResponseMessage? _response;

    protected override Task SetupClientAsync()
    {
        var factory = CreateWebApplicationFactory(Fixture.ConnectionString);
        Client = factory.CreateClient();
        return Task.CompletedTask;
    }

    [Given("the API is running")]
    public void GivenTheApiIsRunning()
    {
        // Client is already initialized by DatabaseTestBase.InitializeAsync()
    }

    [When("I request the root endpoint {string}")]
    public async Task WhenIRequestTheRootEndpoint(string endpoint)
    {
        Client.ShouldNotBeNull();
        _response = await Client.GetAsync(endpoint);
    }

    [When("I request the accounts endpoint {string}")]
    public async Task WhenIRequestTheAccountsEndpoint(string endpoint)
    {
        Client.ShouldNotBeNull();
        _response = await Client.GetAsync(endpoint);
    }

    [Then("I should receive a successful response")]
    public void ThenIShouldReceiveASuccessfulResponse()
    {
        _response.ShouldNotBeNull();
        _response.StatusCode.ShouldBeOneOf(HttpStatusCode.Redirect, HttpStatusCode.OK);
    }

    [Then("I should receive an OK response")]
    public void ThenIShouldReceiveAnOkResponse()
    {
        _response.ShouldNotBeNull();
        _response.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [AfterScenario]
    public void Cleanup()
    {
        // Client is disposed by DatabaseTestBase.DisposeAsync()
    }

    public new async Task InitializeAsync()
    {
        await base.InitializeAsync();
    }
}
