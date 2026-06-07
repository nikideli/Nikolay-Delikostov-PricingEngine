namespace PricingEngine.Domain.Exceptions;

public sealed class ProductNotFoundException : Exception
{
    public string ProductCode { get; }

    public ProductNotFoundException(string productCode)
        : base($"No pricing strategy is registered for product code '{productCode}'.")
    {
        ProductCode = productCode;
    }
}
