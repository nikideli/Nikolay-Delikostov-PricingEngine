using System.ComponentModel.DataAnnotations;

namespace PricingEngine.Application.Configs;

public sealed record UpsertConfigRequest(
    [Required, MinLength(1)] string Value);
