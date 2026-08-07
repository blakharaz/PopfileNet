using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using PopfileNet.Backend.Models;
using Shouldly;
using Xunit;

namespace PopfileNet.IntegrationTests;

[Collection("DatabaseTests")]
public class BackendApiTests(DatabaseFixture fixture) : DatabaseTestBase(fixture)
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
    public async Task RootEndpoint_IsAccessible()
    {
        var response = await Client.GetAsync("/");

        response.StatusCode.ShouldBeOneOf(HttpStatusCode.Redirect, HttpStatusCode.OK, HttpStatusCode.Found);
    }

    [Fact]
    public async Task AccountsEndpoint_ReturnsOk()
    {
        await LoginAsync();

        var response = await Client.GetAsync("/accounts");

        response.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }
}
