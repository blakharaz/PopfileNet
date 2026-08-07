using Microsoft.AspNetCore.Http;

namespace PopfileNet.Ui.Services;

public class CookieForwardingHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var cookieHeader = httpContext.Request.Headers.Cookie;
            if (!string.IsNullOrEmpty(cookieHeader))
            {
                request.Headers.TryAddWithoutValidation("Cookie", cookieHeader.ToString());
            }
        }
        return await base.SendAsync(request, cancellationToken);
    }
}
