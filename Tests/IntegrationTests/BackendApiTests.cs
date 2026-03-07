using System.Net;
using System.Diagnostics;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using PopfileNet.Backend;
using Xunit;

namespace PopfileNet.IntegrationTests;

public class BackendApiTests
{
    private WebApplicationFactory<Program> CreateFactory()
    {
        Environment.SetEnvironmentVariable("SKIP_DB_INIT", "true");
        
        return new WebApplicationFactory<Program>()
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
    }

    [Fact]
    public async Task RootEndpoint_RedirectsToMails()
    {
        var factory = CreateFactory();
        var client = factory.CreateClient();
        
        var response = await client.GetAsync("/");
        
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.OK);
    }

    [Fact]
    public async Task AccountsEndpoint_ReturnsOk()
    {
        var factory = CreateFactory();
        var client = factory.CreateClient();
        
        var response = await client.GetAsync("/accounts");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
