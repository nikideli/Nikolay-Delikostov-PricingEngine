using System.Text.Json.Nodes;

namespace PricingEngine.Domain.Strategies;

/// <summary>
/// Everything a pricing strategy needs to perform its calculation:
/// - Inputs: the product-specific JSON payload from the API caller.
/// - ConfigValues: tariff rates, fees, coefficients loaded from the database for this product.
///
/// The strategy is a pure function: given identical context it always produces
/// identical output — deterministic and side-effect-free.
/// </summary>
public sealed record PricingContext(
    JsonObject Inputs,
    IReadOnlyDictionary<string, string> ConfigValues)
{
    /// <summary>Reads a required decimal input field, throwing InvalidInputException on failure.</summary>
    public decimal GetRequiredDecimal(string key)
    {
        if (!Inputs.TryGetPropertyValue(key, out var node) || node is null)
            throw new Exceptions.InvalidInputException(key, "field is required");

        if (!decimal.TryParse(node.ToString(), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var value))
            throw new Exceptions.InvalidInputException(key, "must be a valid decimal number");

        return value;
    }

    /// <summary>Reads a required integer input field.</summary>
    public int GetRequiredInt(string key)
    {
        if (!Inputs.TryGetPropertyValue(key, out var node) || node is null)
            throw new Exceptions.InvalidInputException(key, "field is required");

        if (!int.TryParse(node.ToString(), out var value))
            throw new Exceptions.InvalidInputException(key, "must be a valid integer");

        return value;
    }

    /// <summary>Reads a required string input field.</summary>
    public string GetRequiredString(string key)
    {
        if (!Inputs.TryGetPropertyValue(key, out var node) || node is null)
            throw new Exceptions.InvalidInputException(key, "field is required");

        return node.GetValue<string>();
    }

    /// <summary>Reads a required decimal config value, throwing if the key is missing.</summary>
    public decimal GetConfigDecimal(string key)
    {
        if (!ConfigValues.TryGetValue(key, out var raw))
            throw new InvalidOperationException($"Missing product config key '{key}'.");

        return decimal.Parse(raw, System.Globalization.CultureInfo.InvariantCulture);
    }
}
