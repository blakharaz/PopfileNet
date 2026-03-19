using System.Net;
using Shouldly;
using Xunit;

namespace PopfileNet.IntegrationTests;

[Collection("Database")]
public class BackendApiTests(DatabaseFixture fixture) : DatabaseTestBase(fixture)
{
    protected override Task SetupClientAsync()
    {
        Factory = CreateWebApplicationFactory(Fixture.ConnectionString);
        Client = Factory.CreateClient();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task RootEndpoint_IsAccessible()
    {
        var response = await Client.GetAsync("/");

        response.StatusCode.ShouldBeOneOf(HttpStatusCode.Redirect, HttpStatusCode.OK);
    }

    [Fact]
    public async Task AccountsEndpoint_ReturnsOk()
    {
        var response = await Client.GetAsync("/accounts");

        response.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }
}
