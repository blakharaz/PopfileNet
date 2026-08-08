using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using PopfileNet.Backend.Models;
using PopfileNet.Common;
using PopfileNet.Database;
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

    private async Task SeedEmailsAsync()
    {
        using var dbContext = Fixture.CreateDbContext();
        var workBucket = new Bucket { Id = Guid.NewGuid().ToString(), Name = "Work" };
        var personalBucket = new Bucket { Id = Guid.NewGuid().ToString(), Name = "Personal" };
        var inbox = new MailFolder { Id = Guid.NewGuid().ToString(), Name = "Inbox", Bucket = workBucket };
        var sent = new MailFolder { Id = Guid.NewGuid().ToString(), Name = "Sent", Bucket = personalBucket };

        dbContext.Buckets.AddRange(workBucket, personalBucket);
        dbContext.MailFolders.AddRange(inbox, sent);
        dbContext.Emails.AddRange(
            CreateEmail("1", "Meeting tomorrow", "Let's meet to discuss the project", inbox),
            CreateEmail("2", "Newsletter", "Buy our products today", sent),
            CreateEmail("3", "Re: sprint planning", "See you at planning", inbox));
        await dbContext.SaveChangesAsync();
    }

    private static Email CreateEmail(string id, string subject, string body, MailFolder folder) => new()
    {
        Id = id,
        Subject = subject,
        Body = body,
        FromAddress = "sender@example.com",
        ToAddresses = "recipient@example.com",
        ReceivedDate = DateTime.UtcNow,
        Folder = folder.Name,
        FolderNavigation = folder
    };

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

    [Fact]
    public async Task Train_PersistsModel_AndRestartLoadsItOnDemand()
    {
        await LoginAsync();
        await SeedEmailsAsync();

        var trainResponse = await Client.PostAsync("/classifier/train", null);
        trainResponse.EnsureSuccessStatusCode();

        var statusAfterTrain = await (await Client.GetAsync("/classifier/status"))
            .Content.ReadFromJsonAsync<ApiResponse<ClassifierStatus>>();
        statusAfterTrain!.Value!.IsTrained.ShouldBeTrue();
        statusAfterTrain.Value.TrainingDataCount.ShouldBe(3);

        // The model artifact must exist on disk under the authenticated user's owner directory.
        var modelPath = Directory
            .GetFiles(ModelsRoot, "model.zip", SearchOption.AllDirectories)
            .SingleOrDefault();
        modelPath.ShouldNotBeNull();

        // Simulate an application restart: a brand new factory shares the same DB + ModelsRoot.
        Client.Dispose();
        await Factory.DisposeAsync();

        Factory = CreateWebApplicationFactory(Fixture.ConnectionString);
        Client = Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        await LoginAsync();

        var statusAfterRestart = await (await Client.GetAsync("/classifier/status"))
            .Content.ReadFromJsonAsync<ApiResponse<ClassifierStatus>>();
        statusAfterRestart!.Value!.IsTrained.ShouldBeTrue();

        var predictResponse = await Client.PostAsJsonAsync("/classifier/predict", new PredictRequest("1"));
        predictResponse.EnsureSuccessStatusCode();
        var prediction = await predictResponse.Content.ReadFromJsonAsync<ApiResponse<PredictionResult>>();
        prediction!.IsSuccess.ShouldBeTrue();
        prediction.Value!.PredictedBucket.ShouldBeOneOf("Work", "Personal");
    }
}
