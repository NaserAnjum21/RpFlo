using FluentAssertions;
using RpFlo.Domain.ValueObjects;

namespace RpFlo.Domain.Tests;

public class MoneyTests
{
    [Fact]
    public void Create_WithValidAmount_ShouldSucceed()
    {
        var money = Money.Create(100.50m);
        money.Amount.Should().Be(100.50m);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Create_WithNegativeAmount_ShouldThrow()
    {
        var act = () => Money.Create(-1);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldRoundToTwoDecimals()
    {
        var money = Money.Create(100.555m);
        money.Amount.Should().Be(100.56m);
    }

    [Fact]
    public void Add_SameCurrency_ShouldSucceed()
    {
        var a = Money.Create(100);
        var b = Money.Create(50.25m);
        var result = a.Add(b);
        result.Amount.Should().Be(150.25m);
    }

    [Fact]
    public void Add_DifferentCurrency_ShouldThrow()
    {
        var a = Money.Create(100, "USD");
        var b = Money.Create(50, "EUR");
        var act = () => a.Add(b);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Multiply_ShouldWork()
    {
        var money = Money.Create(25.50m);
        var result = money.Multiply(3);
        result.Amount.Should().Be(76.50m);
    }

    [Fact]
    public void Zero_ShouldReturnZeroAmount()
    {
        var money = Money.Zero();
        money.Amount.Should().Be(0);
    }
}
