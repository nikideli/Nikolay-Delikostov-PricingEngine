using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace PricingEngine.Api.Controllers;

/// <summary>
/// Minimal health endpoint — useful for container readiness probes.
/// </summary>
public static class HealthEndpoints
{
    public static WebApplication MapHealthEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions { AllowCachingResponses = false });
        return app;
    }
}
