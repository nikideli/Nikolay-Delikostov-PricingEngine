using System.Text.Json.Nodes;

namespace PricingEngine.Application.Quotes;

/// <summary>
/// Inbound DTO from the API layer.
/// <c>Inputs</c> is an open JSON object — the schema is product-specific and validated
/// inside the strategy, not here. This keeps the application service decoupled from
/// any individual product definition.
/// </summary>
public sealed record CreateQuoteRequest(
    string ProductCode,
    JsonObject Inputs);
