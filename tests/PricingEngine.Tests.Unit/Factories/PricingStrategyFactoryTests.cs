using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PricingEngine.Domain.Exceptions;
using PricingEngine.Domain.Strategies;
using PricingEngine.Infrastructure.Factories;

namespace PricingEngine.Tests.Unit.Factories;

public sealed class PricingStrategyFactoryTests
{
    [Fact]
    public void GetStrategy_RegisteredProductCode_ReturnsCorrectStrategy()
    {
        var home = MockStrategy("HOME_BASIC");
        var factory = new PricingStrategyFactory(new[] { home.Object });

        var result = factory.GetStrategy("HOME_BASIC");

        result.Should().BeSameAs(home.Object);
    }

    [Fact]
    public void GetStrategy_IsCaseInsensitive()
    {
        var home = MockStrategy("HOME_BASIC");
        var factory = new PricingStrategyFactory(new[] { home.Object });

        factory.GetStrategy("home_basic").Should().BeSameAs(home.Object);
        factory.GetStrategy("Home_Basic").Should().BeSameAs(home.Object);
    }

    [Fact]
    public void GetStrategy_UnknownProductCode_ThrowsProductNotFoundException()
    {
        var factory = new PricingStrategyFactory(Enumerable.Empty<IPricingStrategy>());

        var act = () => factory.GetStrategy("UNKNOWN_PRODUCT");

        act.Should().Throw<ProductNotFoundException>()
           .Which.ProductCode.Should().Be("UNKNOWN_PRODUCT");
    }

    [Fact]
    public void GetStrategy_MultipleStrategiesRegistered_ResolvesCorrectOne()
    {
        var home = MockStrategy("HOME_BASIC");
        var motor = MockStrategy("MOTOR_COMPREHENSIVE");
        var travel = MockStrategy("TRAVEL_BASIC");

        var factory = new PricingStrategyFactory(new[] { home.Object, motor.Object, travel.Object });

        factory.GetStrategy("MOTOR_COMPREHENSIVE").Should().BeSameAs(motor.Object);
        factory.GetStrategy("TRAVEL_BASIC").Should().BeSameAs(travel.Object);
    }

    [Fact]
    public void GetAllProductCodes_ReturnsSortedListOfRegisteredCodes()
    {
        var factory = new PricingStrategyFactory(new[]
        {
            MockStrategy("TRAVEL_BASIC").Object,
            MockStrategy("HOME_BASIC").Object,
            MockStrategy("MOTOR_COMPREHENSIVE").Object,
        });

        var codes = factory.GetAllProductCodes();

        codes.Should().BeInAscendingOrder();
        codes.Should().BeEquivalentTo(new[] { "HOME_BASIC", "MOTOR_COMPREHENSIVE", "TRAVEL_BASIC" });
    }

    private static Mock<IPricingStrategy> MockStrategy(string productCode)
    {
        var mock = new Mock<IPricingStrategy>();
        mock.Setup(s => s.ProductCode).Returns(productCode);
        return mock;
    }
}
