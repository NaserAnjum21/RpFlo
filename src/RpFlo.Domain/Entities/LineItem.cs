using RpFlo.Domain.Common;
using RpFlo.Domain.ValueObjects;

namespace RpFlo.Domain.Entities;

public sealed class LineItem : Entity
{
    public string Name { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; } = Money.Zero();
    public Money TotalPrice => UnitPrice.Multiply(Quantity);
    public Guid ProcurementRequestId { get; private init; }

    private LineItem() { }

    public static LineItem Create(string name, int quantity, decimal unitPrice, Guid requestId) =>
        new()
        {
            Name = name,
            Quantity = quantity,
            UnitPrice = Money.Create(unitPrice),
            ProcurementRequestId = requestId
        };

    public void Update(string name, int quantity, decimal unitPrice)
    {
        Name = name;
        Quantity = quantity;
        UnitPrice = Money.Create(unitPrice);
        Touch();
    }
}
