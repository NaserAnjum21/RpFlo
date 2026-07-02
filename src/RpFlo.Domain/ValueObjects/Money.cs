namespace RpFlo.Domain.ValueObjects;

public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, string currency = "USD") =>
        amount < 0
            ? throw new ArgumentException("Amount cannot be negative.", nameof(amount))
            : new Money(Math.Round(amount, 2), currency);

    public static Money Zero(string currency = "USD") => new(0, currency);

    public Money Add(Money other) =>
        Currency != other.Currency
            ? throw new InvalidOperationException("Cannot add different currencies.")
            : new Money(Amount + other.Amount, Currency);

    public Money Multiply(decimal factor) =>
        new(Math.Round(Amount * factor, 2), Currency);

    public override string ToString() => $"{Currency} {Amount:N2}";
}
