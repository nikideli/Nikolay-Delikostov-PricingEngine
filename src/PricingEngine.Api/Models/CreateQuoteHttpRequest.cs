using System.Text.Json.Nodes;
using System.ComponentModel.DataAnnotations;

namespace PricingEngine.Api.Models;

/// <summary>
/// Inbound HTTP request. The <c>inputs</c> field is an open JSON object;
/// its required fields are validated inside the pricing strategy, not here,
/// because each product defines its own input schema.
/// </summary>
public sealed class CreateQuoteHttpRequest
{
    [Required]
    public string ProductCode { get; set; } = string.Empty;

    [Required]
    public JsonObject Inputs { get; set; } = new();
}
