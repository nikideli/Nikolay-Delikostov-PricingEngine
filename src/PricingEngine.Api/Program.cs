using Microsoft.EntityFrameworkCore;
using Serilog;
using PricingEngine.Api.Admin;
using PricingEngine.Api.Middleware;
using PricingEngine.Infrastructure.Data;
using PricingEngine.Infrastructure.Extensions;

// Bootstrap Serilog before anything else so startup errors are also captured
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // --- Serilog ---
    builder.Host.UseSerilog((ctx, services, config) =>
        config.ReadFrom.Configuration(ctx.Configuration)
              .ReadFrom.Services(services)
              .WriteTo.Console(outputTemplate:
                  "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"));

    // --- Controllers + Swagger ---
    builder.Services.AddControllers()
        .AddJsonOptions(opts =>
        {
            // Use camelCase for all response properties
            opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            // Don't output null fields in responses
            opts.JsonSerializerOptions.DefaultIgnoreCondition =
                System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new()
        {
            Title = "Pricing Engine API",
            Version = "v1",
            Description = "Public endpoints for creating and retrieving insurance quotes."
        });

        c.SwaggerDoc("admin", new()
        {
            Title = "Pricing Engine — Admin API",
            Version = "admin",
            Description = "Internal endpoints for managing product configurations and viewing registered products. " +
                          "Requires the X-Admin-Key header when Admin:ApiKey is configured."
        });

        // Only include each controller in its own document
        c.DocInclusionPredicate((docName, apiDesc) =>
            (apiDesc.GroupName ?? "v1") == docName);

        // Injects the X-Admin-Key header field on every admin operation
        c.OperationFilter<AdminApiKeyOperationFilter>();
    });

    // AdminApiKeyFilter is resolved from DI so it can receive IConfiguration
    builder.Services.AddScoped<AdminApiKeyFilter>();

    // --- Health checks ---
    builder.Services.AddHealthChecks();

    // --- Error handling middleware ---
    builder.Services.AddTransient<ErrorHandlingMiddleware>();

    // --- Infrastructure (DB, Repos, Strategies, MassTransit, Outbox) ---
    builder.Services.AddInfrastructure(builder.Configuration);

    var app = builder.Build();

    // --- Auto-apply EF Core migrations on startup ---
    await using (var scope = app.Services.CreateAsyncScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<PricingEngineDbContext>();
        await db.Database.MigrateAsync();
    }

    // --- Middleware pipeline ---
    app.UseMiddleware<ErrorHandlingMiddleware>();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Public API v1");
        c.SwaggerEndpoint("/swagger/admin/swagger.json", "Admin API");
    });

    app.MapControllers();
    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}

// Expose the Program class for integration tests
public partial class Program { }
