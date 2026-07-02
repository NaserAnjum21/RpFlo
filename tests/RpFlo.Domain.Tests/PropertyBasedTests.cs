using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using RpFlo.Domain.Entities;
using RpFlo.Domain.Enums;
using RpFlo.Domain.ValueObjects;

namespace RpFlo.Domain.Tests;

public class PropertyBasedTests
{
    [Property]
    public bool Money_Create_NeverNegative(PositiveInt amount)
    {
        var money = Money.Create((decimal)amount.Get / 100);
        return money.Amount >= 0;
    }

    [Property]
    public bool Money_Add_IsCommutative(PositiveInt a, PositiveInt b)
    {
        var m1 = Money.Create((decimal)a.Get / 100);
        var m2 = Money.Create((decimal)b.Get / 100);
        return m1.Add(m2).Amount == m2.Add(m1).Amount;
    }

    [Property]
    public bool Money_Multiply_PreservesNonNegative(PositiveInt amount, PositiveInt factor)
    {
        var money = Money.Create((decimal)amount.Get / 100);
        var result = money.Multiply(factor.Get);
        return result.Amount >= 0;
    }

    [Property]
    public bool LineItem_TotalPrice_EqualsQuantityTimesUnitPrice(PositiveInt qty, PositiveInt price)
    {
        var unitPrice = (decimal)price.Get / 100;
        var item = LineItem.Create("Test", qty.Get, unitPrice, Guid.NewGuid());
        var expected = Money.Create(unitPrice).Multiply(qty.Get).Amount;
        return item.TotalPrice.Amount == expected;
    }

    [Property]
    public bool ProcurementRequest_CannotIssuePO_FromDraft()
    {
        var pr = ProcurementRequest.Create("Test", "Desc", Department.Engineering, Urgency.Low, Guid.NewGuid());
        pr.AddLineItem("Item", 1, 100m);
        return pr.IssuePurchaseOrder(Guid.NewGuid()).IsFailure;
    }

    [Property]
    public bool ProcurementRequest_CannotReject_FromDraft()
    {
        var pr = ProcurementRequest.Create("Test", "Desc", Department.Engineering, Urgency.Low, Guid.NewGuid());
        pr.AddLineItem("Item", 1, 100m);
        return pr.RejectByManager(Guid.NewGuid(), "reason").IsFailure;
    }

    [Fact]
    public void ProcurementRequest_TotalAmount_SumsCorrectly_ForVariousCounts()
    {
        for (var count = 1; count <= 10; count++)
        {
            var pr = ProcurementRequest.Create("Test", "Desc", Department.Engineering, Urgency.Low, Guid.NewGuid());
            var expectedTotal = 0m;

            for (var i = 0; i < count; i++)
            {
                var qty = i + 1;
                var price = (i + 1) * 10m;
                pr.AddLineItem($"Item {i}", qty, price);
                expectedTotal += Money.Create(price).Multiply(qty).Amount;
            }

            pr.TotalAmount.Amount.Should().Be(expectedTotal);
        }
    }
}
