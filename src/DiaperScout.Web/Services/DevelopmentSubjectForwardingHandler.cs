namespace DiaperScout.Web.Services;

/// <summary>Forwards the existing development-only test identity to the API; production credentials are never forwarded here.</summary>
public sealed class DevelopmentSubjectForwardingHandler(IHttpContextAccessor httpContextAccessor, IHostEnvironment environment, IConfiguration configuration) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (environment.IsDevelopment())
        {
            var subject = httpContextAccessor.HttpContext?.Request.Headers["X-Development-Subject"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(subject))
                subject = configuration["Authentication:Development:Subject"];

            if (!string.IsNullOrWhiteSpace(subject))
                request.Headers.TryAddWithoutValidation("X-Development-Subject", subject);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
