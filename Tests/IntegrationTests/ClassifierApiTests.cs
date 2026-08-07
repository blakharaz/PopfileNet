using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using PopfileNet.Backend.Models;
using Shouldly;
using Xunit;

namespace PopfileNet.IntegrationTests;

[Collection("DatabaseTests")]
public class ClassifierApiTests(DatabaseFixture fixture) : DatabaseTestBase(fixture)
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
    public async Task GetStatus_ReturnsNotTrained()
    {
        await LoginAsync();
        var response = await Client.GetAsync("/classifier/status");

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
        await LoginAsync();
        var response = await Client.PostAsync("/classifier/train", null);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
        content.ShouldNotBeNull();
        content.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task Predict_WithoutTraining_ReturnsSuccessWithEmptyResult()
    {
        await LoginAsync();
        var response = await Client.PostAsJsonAsync("/classifier/predict", new PredictRequest("some-email-id"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<PredictionResult>>();
        content.ShouldNotBeNull();
        content.IsSuccess.ShouldBeTrue();
        content.Value.ShouldNotBeNull();
        content.Value.PredictedBucket.ShouldBeEmpty();
    }
}
