using Microsoft.AspNetCore.Http;
using Moq;
using PopfileNet.Ui.Services;
using Shouldly;
using Xunit;

namespace PopfileNet.Ui.UnitTests.Services;

public class CookieForwardingHandlerTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;

    public CookieForwardingHandlerTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
    }

    [Fact]
    public async Task SendAsync_WithHttpContextAndCookie_ForwardsCookieHeader()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Cookie = "session=abc123";
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

        var capturedRequest = (HttpRequestMessage?)null;
        var innerHandler = new CapturingHandler(message => capturedRequest = message);
        var handler = new CookieForwardingHandler(_httpContextAccessorMock.Object)
        {
            InnerHandler = innerHandler
        };
        var invoker = new HttpMessageInvoker(handler);

        using var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/users");
        await invoker.SendAsync(request, CancellationToken.None);

        capturedRequest.ShouldNotBeNull();
        request.Headers.TryGetValues("Cookie", out var values).ShouldBeTrue();
        values.ShouldContain("session=abc123");
    }

    [Fact]
    public async Task SendAsync_WithContextButNoCookie_DoesNotAddCookieHeader()
    {
        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

        var handler = new CookieForwardingHandler(_httpContextAccessorMock.Object)
        {
            InnerHandler = new PassThroughHandler()
        };

        var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost/api/users");
        await invoker.SendAsync(request, CancellationToken.None);

        request.Headers.TryGetValues("Cookie", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task SendAsync_WithNoHttpContext_DoesNotAddCookieHeader()
    {
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns((HttpContext?)null);

        var handler = new CookieForwardingHandler(_httpContextAccessorMock.Object)
        {
            InnerHandler = new PassThroughHandler()
        };

        var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost/api/users");
        await invoker.SendAsync(request, CancellationToken.None);

        request.Headers.TryGetValues("Cookie", out _).ShouldBeFalse();
    }

    private class CapturingHandler(Action<HttpRequestMessage> onRequest) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            onRequest(request);
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }

    private class PassThroughHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
    }
}