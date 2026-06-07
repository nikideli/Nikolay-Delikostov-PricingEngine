using System.Text.Json.Nodes;
using FluentAssertions;
using PricingEngine.Domain.Exceptions;
using PricingEngine.Domain.Strategies;

namespace PricingEngine.Tests.Unit.Strategies;

/// <summary>
/// Tests the HomeBasicPricingStrategy in isolation.
/// These are pure unit tests — no database, no I/O.
/// </summary>
public sealed class HomeBasicPricingStrategyTests
{
    private static readonly Dictionary<string, string> ValidConfigs = new()
    {
        ["tariff_rate"] = "0.0018",
        ["fixed_fee"] = "25.00",
        ["tax_rate"] = "0.12",
        ["installment_fee_2x"] = "0.015",
        ["installment_fee_4x"] = "0.030",
    };

    private readonly HomeBasicPricingStrategy _sut = new();

    [Fact]
    public async Task CalculateAsync_TypicalInsuredSum_ReturnsCorrectBreakdown()
    {
        // Arrange
        var inputs = BuildInputs(insuredSum: 100_000m);
        var context = new PricingContext(inputs, ValidConfigs);

        // Act
        var result = await _sut.CalculateAsync(context);

        // Assert
        // NetPremium = 100,000 × 0.0018 = 180.00
        result.Breakdown.NetPremium.Should().Be(180.00m);

        // Taxes = 180.00 × 0.12 = 21.60
        result.Breakdown.Taxes.Should().Be(21.60m);

        // Fees = 25.00 (flat)
        result.Breakdown.Fees.Should().Be(25.00m);

        // Total = 180.00 + 21.60 + 25.00 = 226.60
        result.Breakdown.Total.Should().Be(226.60m);
    }

    [Fact]
    public async Task CalculateAsync_ReturnsThreeInstallmentPlans()
    {
        var context = new PricingContext(BuildInputs(100_000m), ValidConfigs);

        var result = await _sut.CalculateAsync(context);

        result.InstallmentPlans.Should().HaveCount(3);
        result.InstallmentPlans.Select(p => p.NumberOfInstallments)
            .Should().BeEquivalentTo(new[] { 1, 2, 4 });
    }

    [Fact]
    public async Task CalculateAsync_SingleInstallment_HasNoSurcharge()
    {
        var context = new PricingContext(BuildInputs(100_000m), ValidConfigs);

        var result = await _sut.CalculateAsync(context);
        var singlePlan = result.InstallmentPlans.Single(p => p.NumberOfInstallments == 1);

        // Total = 226.60, no surcharge
        singlePlan.TotalAmount.Should().Be(226.60m);
        singlePlan.AmountPerInstallment.Should().Be(226.60m);
    }

    [Fact]
    public async Task CalculateAsync_TwoInstallments_AppliesSurcharge()
    {
        var context = new PricingContext(BuildInputs(100_000m), ValidConfigs);

        var result = await _sut.CalculateAsync(context);
        var twoPlan = result.InstallmentPlans.Single(p => p.NumberOfInstallments == 2);

        // 226.60 × 1.015 = 229.999 → rounds AwayFromZero to 230.00
        twoPlan.TotalAmount.Should().Be(230.00m);
        twoPlan.TotalAmount.Should().BeGreaterThan(226.60m);
    }

    [Fact]
    public async Task CalculateAsync_FourInstallments_AppliesHigherSurcharge()
    {
        var context = new PricingContext(BuildInputs(100_000m), ValidConfigs);
        var result = await _sut.CalculateAsync(context);
        var fourPlan = result.InstallmentPlans.Single(p => p.NumberOfInstallments == 4);

        // 226.60 × 1.030 = 233.398 → 233.40
        fourPlan.TotalAmount.Should().Be(233.40m);
        fourPlan.AmountPerInstallment.Should().Be(decimal.Round(233.40m / 4, 2, MidpointRounding.AwayFromZero));
    }

    [Fact]
    public async Task CalculateAsync_MissingInsuredSum_ThrowsInvalidInputException()
    {
        var inputs = new JsonObject();  // no insuredSum
        var context = new PricingContext(inputs, ValidConfigs);

        var act = () => _sut.CalculateAsync(context);

        await act.Should().ThrowAsync<InvalidInputException>()
            .WithMessage("*insuredSum*");
    }

    [Fact]
    public async Task CalculateAsync_ZeroInsuredSum_ThrowsInvalidInputException()
    {
        var inputs = BuildInputs(insuredSum: 0m);
        var context = new PricingContext(inputs, ValidConfigs);

        var act = () => _sut.CalculateAsync(context);

        await act.Should().ThrowAsync<InvalidInputException>();
    }

    [Fact]
    public async Task CalculateAsync_NegativeInsuredSum_ThrowsInvalidInputException()
    {
        var inputs = BuildInputs(insuredSum: -500m);
        var context = new PricingContext(inputs, ValidConfigs);

        var act = () => _sut.CalculateAsync(context);

        await act.Should().ThrowAsync<InvalidInputException>();
    }

    [Theory]
    [InlineData(50_000, 90.00, 10.80, 25.00, 125.80)]
    [InlineData(250_000, 450.00, 54.00, 25.00, 529.00)]
    [InlineData(500_000, 900.00, 108.00, 25.00, 1033.00)]
    public async Task CalculateAsync_VariousInsuredSums_ProducesCorrectTotals(
        decimal insuredSum, decimal expectedNet, decimal expectedTax, decimal expectedFee, decimal expectedTotal)
    {
        var context = new PricingContext(BuildInputs(insuredSum), ValidConfigs);

        var result = await _sut.CalculateAsync(context);

        result.Breakdown.NetPremium.Should().Be(expectedNet);
        result.Breakdown.Taxes.Should().Be(expectedTax);
        result.Breakdown.Fees.Should().Be(expectedFee);
        result.Breakdown.Total.Should().Be(expectedTotal);
    }

    [Fact]
    public void ProductCode_IsHomeBasic()
    {
        _sut.ProductCode.Should().Be("HOME_BASIC");
    }

    // ---- Helpers ----

    private static JsonObject BuildInputs(decimal insuredSum) =>
        new() { ["insuredSum"] = insuredSum };
}
