using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PricingEngine.Api.Admin;

/// <summary>
/// Injects the X-Admin-Key header parameter into every operation that belongs
/// to the "admin" Swagger document so the Swagger UI renders the key field
/// without requiring it on public endpoints.
/// </summary>
public sealed class AdminApiKeyOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.GroupName != "admin")
            return;

        operation.Parameters ??= [];
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = AdminApiKeyFilter.HeaderName,
            In = ParameterLocation.Header,
            Required = false,
            Description = "Admin API key. Required when Admin:ApiKey is set in server configuration.",
            Schema = new OpenApiSchema { Type = "string" }
        });
    }
}
