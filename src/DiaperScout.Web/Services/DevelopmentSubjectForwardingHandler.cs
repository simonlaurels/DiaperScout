namespace DiaperScout.Web.Services;

/// <summary>
/// Forwards the development-only moderator identity to the API when the browser
/// is authenticated. Production credentials are never forwarded here.
/// </summary>
public sealed class DevelopmentSubjectForwardingHandler(
    IHttpContextAccessor httpContextAccessor,
    IHostEnvironment environment,
    IConfiguration configuration) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (environment.IsDevelopment())
        {
            var httpContext = httpContextAccessor.HttpContext;
            var user = httpContext?.User;

            if (user?.Identity?.IsAuthenticated == true &&
                (user.IsInRole("Moderator") || user.IsInRole("Administrator")))
            {
                var subject = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? configuration["Authentication:Development:Subject"];

                if (!string.IsNullOrWhiteSpace(subject))
                    request.Headers.TryAddWithoutValidation(
                        "X-Development-Subject",
                        subject);
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}
