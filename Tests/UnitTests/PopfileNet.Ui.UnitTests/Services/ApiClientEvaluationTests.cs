using System.Net; using System.Text.Json; using PopfileNet.Ui.Services; using Shouldly; using Xunit;

namespace PopfileNet.Ui.UnitTests.Services;

public sealed class ApiClientEvaluationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static HttpClient CreateMockClient(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        var mockHandler = new MockHttpHandler(handler);
        return new HttpClient(mockHandler) { BaseAddress = new Uri("http://localhost") };
    }

    [Fact]
    public async Task GetDevModeStatusAsync_ReturnsTrue_WhenServerReturnsTrue()
    {
        var client = CreateMockClient(async (request, ct) =>
        {
            var response = new ApiResponse<string> { Value = "True" };
            var content = JsonSerializer.Serialize(response, JsonOptions);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            };
        });

        var apiClient = new ApiClient(client);

        var result = await apiClient.GetDevModeStatusAsync();

        result!.Value.ShouldBeTrue();
    }

    [Fact]
    public async Task GetDevModeStatusAsync_ReturnsFalse_WhenServerReturnsFalse()
    {
        var client = CreateMockClient(async (request, ct) =>
        {
            var response = new ApiResponse<string> { Value = "False" };
            var content = JsonSerializer.Serialize(response, JsonOptions);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            };
        });

        var apiClient = new ApiClient(client);

        var result = await apiClient.GetDevModeStatusAsync();

        result!.Value.ShouldBeFalse();
    }

    [Fact]
    public async Task GetDevModeStatusAsync_ReturnsFalse_WhenServerReturnsInvalid()
    {
        var client = CreateMockClient(async (request, ct) =>
        {
            var response = new ApiResponse<string> { Value = "invalid" };
            var content = JsonSerializer.Serialize(response, JsonOptions);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            };
        });

        var apiClient = new ApiClient(client);

        var result = await apiClient.GetDevModeStatusAsync();

        result!.Value.ShouldBeFalse();
    }

    [Fact]
    public async Task GetDevModeStatusAsync_ReturnsFalse_WhenValueIsNull()
    {
        var client = CreateMockClient(async (request, ct) =>
        {
            var response = new ApiResponse<string> { Value = null };
            var content = JsonSerializer.Serialize(response, JsonOptions);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            };
        });

        var apiClient = new ApiClient(client);

        var result = await apiClient.GetDevModeStatusAsync();

        result!.Value.ShouldBeFalse();
    }

    [Fact]
    public async Task RunEvaluationAsync_ReturnsResult_OnSuccess()
    {
        var expectedResult = new EvaluationResult
        {
            NumberOfRuns = 1,
            Runs =
            [
                new RunResultDto(1, 10, 5, 0.8f, 4, 5, [], [])
            ]
        };

        var client = CreateMockClient(async (request, ct) =>
        {
            var response = new ApiResponse<EvaluationResult> { Value = expectedResult };
            var content = JsonSerializer.Serialize(response, JsonOptions);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            };
        });

        var apiClient = new ApiClient(client);

        var result = await apiClient.RunEvaluationAsync(new EvaluationRequest());

        result.ShouldNotBeNull();
        result.NumberOfRuns.ShouldBe(1);
    }

    [Fact]
    public async Task RunEvaluationAsync_WithMultiRun_ReturnsAggregated()
    {
        var expectedResult = new EvaluationResult
        {
            NumberOfRuns = 2,
            Runs =
            [
                new RunResultDto(1, 10, 5, 0.8f, 4, 5, [], []),
                new RunResultDto(2, 10, 5, 0.9f, 5, 5, [], [])
            ],
            Aggregated = new AggregatedMetricsDto(0.85f, 0.8f, 0.9f, null)
        };

        var client = CreateMockClient(async (request, ct) =>
        {
            var response = new ApiResponse<EvaluationResult> { Value = expectedResult };
            var content = JsonSerializer.Serialize(response, JsonOptions);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            };
        });

        var apiClient = new ApiClient(client);

        var result = await apiClient.RunEvaluationAsync(
            new EvaluationRequest { NumberOfRuns = 2 });

        result.ShouldNotBeNull();
        result.NumberOfRuns.ShouldBe(2);
        result.Aggregated.ShouldNotBeNull();
        result.Aggregated.MeanAccuracy.ShouldBe(0.85f);
    }

    [Fact]
    public async Task GetEvaluationConfigAsync_ReturnsConfig_OnSuccess()
    {
        var expectedConfig = new
        {
            Folders = new[] { "Inbox", "Sent" },
            Buckets = new[] { new { Id = "1", Name = "Work" } }
        };

        var client = CreateMockClient(async (request, ct) =>
        {
            var response = new ApiResponse<object> { Value = expectedConfig };
            var content = JsonSerializer.Serialize(response, JsonOptions);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            };
        });

        var apiClient = new ApiClient(client);

        var result = await apiClient.GetEvaluationConfigAsync();

        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetEvaluationConfigAsync_ReturnsNull_OnFailure()
    {
        var client = CreateMockClient(async (request, ct) =>
        {
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        });

        var apiClient = new ApiClient(client);

        await Should.ThrowAsync<HttpRequestException>(
            () => apiClient.GetEvaluationConfigAsync());
    }

    private sealed class MockHttpHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;
        public MockHttpHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) => _handler = handler;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            await _handler(request, ct);
    }
}
