using System.Net;
using System.Net.Http.Json;
using PopfileNet.Backend.Models;
using Shouldly;
using Xunit;

namespace PopfileNet.IntegrationTests;

[Collection("DatabaseTests")]
public class ClassifierApiTests(DatabaseFixture fixture) : DatabaseTestBase(fixture)
{
    protected override Task SetupClientAsync()
    {
        Factory = CreateWebApplicationFactory(Fixture.ConnectionString);
        Client = Factory.CreateClient();
        return Task.CompletedTask;
    }
    
    [Fact]
    public async Task GetStatus_ReturnsNotTrained()
    {
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
        var response = await Client.PostAsync("/classifier/train", null);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
        content.ShouldNotBeNull();
        content.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task Predict_WithoutTraining_ReturnsSuccessWithEmptyResult()
    {
        var response = await Client.PostAsJsonAsync("/classifier/predict", new PredictRequest("some-email-id"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<PredictionResult>>();
        content.ShouldNotBeNull();
        content.IsSuccess.ShouldBeTrue();
        content.Value.ShouldNotBeNull();
        content.Value.PredictedBucket.ShouldBeEmpty();
    }
}
