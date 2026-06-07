using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PricingEngine.Api.Admin;

/// <summary>
/// Action filter that enforces the X-Admin-Key header on all admin endpoints.
///
/// When Admin:ApiKey is set in configuration every request to an admin controller
/// must supply a matching X-Admin-Key header.
/// When the config key is absent (e.g. local development without secrets) the
/// filter is a no-op so the team can work without setting up keys locally.
/// </summary>
public sealed class AdminApiKeyFilter : IAsyncActionFilter
{
    public const string HeaderName = "X-Admin-Key";
    private readonly IConfiguration _config;

    public AdminApiKeyFilter(IConfiguration config) => _config = config;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var configuredKey = _config["Admin:ApiKey"];

        // No key configured → open access (useful for local dev / test environments)
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            await next();
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var providedKey)
            || !string.Equals(configuredKey, providedKey, StringComparison.Ordinal))
        {
            context.Result = new ObjectResult(new { error = "Invalid or missing X-Admin-Key header." })
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
            return;
        }

        await next();
    }
}
