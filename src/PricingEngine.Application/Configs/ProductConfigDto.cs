namespace PricingEngine.Application.Configs;

public sealed record ProductConfigDto(
    Guid Id,
    string ProductCode,
    string Key,
    string Value,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo);
