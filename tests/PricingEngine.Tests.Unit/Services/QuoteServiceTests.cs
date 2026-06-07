using System.Text.Json.Nodes;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PricingEngine.Application.Events;
using PricingEngine.Application.Interfaces;
using PricingEngine.Application.Quotes;
using PricingEngine.Domain.Entities;
using PricingEngine.Domain.Exceptions;
using PricingEngine.Domain.Strategies;
using PricingEngine.Domain.ValueObjects;

namespace PricingEngine.Tests.Unit.Services;

public sealed class QuoteServiceTests
{
    private readonly Mock<IPricingStrategyFactory> _strategyFactory = new();
    private readonly Mock<IProductConfigRepository> _configRepo = new();
    private readonly Mock<IQuoteRepository> _quoteRepo = new();
    private readonly Mock<IOutboxRepository> _outboxRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private QuoteService CreateSut() => new(
        _strategyFactory.Object,
        _configRepo.Object,
        _quoteRepo.Object,
        _outboxRepo.Object,
        _unitOfWork.Object,
        NullLogger<QuoteService>.Instance);

    [Fact]
    public async Task CreateQuoteAsync_ValidRequest_ReturnsPopulatedResponse()
    {
        // Arrange
        var inputs = new JsonObject { ["insuredSum"] = 250_000m };
        var request = new CreateQuoteRequest("HOME_BASIC", inputs);

        var expectedBreakdown = new PriceBreakdown(450m, 54m, 25m);
        var expectedPlans = new[]
        {
            new InstallmentPlan(1, 529m, 529m),
            new InstallmentPlan(2, 268.48m, 536.96m),
            new InstallmentPlan(4, 136.50m, 546m),
        };

        var mockStrategy = new Mock<IPricingStrategy>();
        mockStrategy.Setup(s => s.CalculateAsync(It.IsAny<PricingContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PricingResult(expectedBreakdown, expectedPlans));

        _strategyFactory.Setup(f => f.GetStrategy("HOME_BASIC")).Returns(mockStrategy.Object);
        _configRepo.Setup(r => r.GetByProductCodeAsync("HOME_BASIC", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ProductConfig>());

        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>((op, _) => op());

        // Act
        var result = await CreateSut().CreateQuoteAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.ProductCode.Should().Be("HOME_BASIC");
        result.QuoteId.Should().NotBeEmpty();
        result.Breakdown.NetPremium.Should().Be(450m);
        result.Breakdown.Taxes.Should().Be(54m);
        result.Breakdown.Total.Should().Be(529m);
        result.InstallmentPlans.Should().HaveCount(3);
    }

    [Fact]
    public async Task CreateQuoteAsync_UnknownProduct_ThrowsProductNotFoundException()
    {
        // Arrange
        _strategyFactory.Setup(f => f.GetStrategy("UNKNOWN"))
            .Throws(new ProductNotFoundException("UNKNOWN"));

        var request = new CreateQuoteRequest("UNKNOWN", new JsonObject());

        // Act
        var act = () => CreateSut().CreateQuoteAsync(request);

        // Assert
        await act.Should().ThrowAsync<ProductNotFoundException>()
            .WithMessage("*UNKNOWN*");
    }

    [Fact]
    public async Task CreateQuoteAsync_PersistsQuoteAndOutboxMessageInSameTransaction()
    {
        // Arrange — this verifies the atomic write guarantee of the outbox pattern
        var inputs = new JsonObject { ["insuredSum"] = 100_000m };
        var request = new CreateQuoteRequest("HOME_BASIC", inputs);

        var breakdown = new PriceBreakdown(180m, 21.60m, 25m);
        var mockStrategy = new Mock<IPricingStrategy>();
        mockStrategy.Setup(s => s.CalculateAsync(It.IsAny<PricingContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PricingResult(breakdown, Array.Empty<InstallmentPlan>()));

        _strategyFactory.Setup(f => f.GetStrategy("HOME_BASIC")).Returns(mockStrategy.Object);
        _configRepo.Setup(r => r.GetByProductCodeAsync("HOME_BASIC", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ProductConfig>());

        // Set up ExecuteInTransactionAsync to actually invoke the callback and record order
        var callOrder = new List<string>();
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>(async (op, _) =>
            {
                callOrder.Add("tx_begin");
                await op();
                callOrder.Add("tx_commit");
            });
        _quoteRepo.Setup(r => r.Add(It.IsAny<Quote>()))
            .Callback(() => callOrder.Add("add_quote"));
        _outboxRepo.Setup(r => r.Add(It.IsAny<string>(), It.IsAny<object>()))
            .Callback(() => callOrder.Add("add_outbox"));

        // Act
        await CreateSut().CreateQuoteAsync(request);

        // Assert: quote and outbox are staged inside the transaction callback
        callOrder.Should().Equal("tx_begin", "add_quote", "add_outbox", "tx_commit");
    }

    [Fact]
    public async Task CreateQuoteAsync_OnTransactionFailure_ExceptionPropagates()
    {
        // Arrange — UnitOfWork rolls back internally; QuoteService just lets the exception surface
        var inputs = new JsonObject { ["insuredSum"] = 100_000m };
        var request = new CreateQuoteRequest("HOME_BASIC", inputs);

        var breakdown = new PriceBreakdown(180m, 21.60m, 25m);
        var mockStrategy = new Mock<IPricingStrategy>();
        mockStrategy.Setup(s => s.CalculateAsync(It.IsAny<PricingContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PricingResult(breakdown, Array.Empty<InstallmentPlan>()));

        _strategyFactory.Setup(f => f.GetStrategy("HOME_BASIC")).Returns(mockStrategy.Object);
        _configRepo.Setup(r => r.GetByProductCodeAsync("HOME_BASIC", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ProductConfig>());

        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB failure"));

        // Act
        var act = () => CreateSut().CreateQuoteAsync(request);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("DB failure");
    }
}
