namespace PricingEngine.Domain.Exceptions;

public sealed class InvalidInputException : Exception
{
    public InvalidInputException(string message) : base(message) { }

    public InvalidInputException(string fieldName, string reason)
        : base($"Input field '{fieldName}' is invalid: {reason}.") { }
}
