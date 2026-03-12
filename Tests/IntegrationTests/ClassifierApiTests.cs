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

public class ClassifierApiTests : IAsyncLifetime
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
    public async Task GetStatus_ReturnsNotTrained()
    {
        var response = await _client!.GetAsync("/classifier/status");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<ClassifierStatus>>();
        content.ShouldNotBeNull();
        content.IsSuccess.ShouldBeTrue();
        content.Value.ShouldNotBeNull();
        content.Value.IsTrained.ShouldBeFalse();
    }

    [Fact]
    public async Task Train_WithNoData_ReturnsBadRequest()
    {
        var response = await _client!.PostAsync("/classifier/train", null);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
        content.ShouldNotBeNull();
        content.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task Predict_WithoutTraining_ReturnsSuccessWithEmptyResult()
    {
        var response = await _client!.PostAsJsonAsync("/classifier/predict", new PredictRequest("some-email-id"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<PredictionResult>>();
        content.ShouldNotBeNull();
        content.IsSuccess.ShouldBeTrue();
        content.Value.ShouldNotBeNull();
        content.Value.PredictedBucket.ShouldBeEmpty();
    }
}
