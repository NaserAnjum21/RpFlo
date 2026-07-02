using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RpFlo.Domain.Common;
using RpFlo.Domain.Entities;
using RpFlo.Domain.Enums;

namespace RpFlo.Infrastructure.Persistence;

public static class SeedData
{
    public static readonly Guid RequesterId1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid RequesterId2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid ManagerId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid FinanceId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    public static readonly Guid AdminId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();

        if (await db.Users.AnyAsync()) return;

        var users = new[]
        {
            CreateUser(RequesterId1, "Alice Johnson", "alice@company.com", UserRole.Requester, Department.Engineering),
            CreateUser(RequesterId2, "Bob Smith", "bob@company.com", UserRole.Requester, Department.Marketing),
            CreateUser(ManagerId, "Carol Williams", "carol@company.com", UserRole.Manager, Department.Operations),
            CreateUser(FinanceId, "Dave Brown", "dave@company.com", UserRole.Finance, Department.Finance),
            CreateUser(AdminId, "Eve Davis", "eve@company.com", UserRole.Admin, Department.Operations),
        };

        await db.Users.AddRangeAsync(users);

        var pr1 = ProcurementRequest.Create(
            "Development Laptops",
            "New MacBook Pro laptops for the engineering team expansion",
            Department.Engineering,
            Urgency.High,
            RequesterId1);
        pr1.AddLineItem("MacBook Pro 16\" M4", 5, 2499.00m);
        pr1.AddLineItem("USB-C Dock", 5, 189.99m);
        pr1.AddLineItem("Monitor Stand", 5, 49.99m);
        pr1.Submit(RequesterId1);

        var pr2 = ProcurementRequest.Create(
            "Marketing Campaign Materials",
            "Print materials and branded merchandise for Q3 campaign",
            Department.Marketing,
            Urgency.Medium,
            RequesterId2);
        pr2.AddLineItem("Branded T-Shirts", 200, 12.50m);
        pr2.AddLineItem("Brochures (1000 pack)", 5, 350.00m);
        pr2.AddLineItem("Banner Stands", 3, 275.00m);
        pr2.Submit(RequesterId2);
        pr2.ApproveByManager(ManagerId, "Approved for Q3 campaign budget");

        var pr3 = ProcurementRequest.Create(
            "Office Supplies Restock",
            "Monthly restocking of office supplies for all departments",
            Department.Operations,
            Urgency.Low,
            RequesterId1);
        pr3.AddLineItem("Copy Paper (Case)", 20, 45.00m);
        pr3.AddLineItem("Ink Cartridges", 10, 32.99m);
        pr3.AddLineItem("Sticky Notes", 50, 3.99m);
        pr3.Submit(RequesterId1);
        pr3.ApproveByManager(ManagerId);
        pr3.ApproveByFinance(FinanceId, "Within monthly office budget");
        pr3.IssuePurchaseOrder(FinanceId);

        var pr4 = ProcurementRequest.Create(
            "Server Upgrade",
            "Upgrade production database server hardware",
            Department.Engineering,
            Urgency.Critical,
            RequesterId1);
        pr4.AddLineItem("Dell PowerEdge R760", 1, 12500.00m);
        pr4.AddLineItem("128GB DDR5 ECC RAM", 4, 899.00m);
        pr4.AddLineItem("NVMe SSD 4TB", 4, 649.00m);
        pr4.Submit(RequesterId1);
        pr4.RejectByManager(ManagerId, "Need updated vendor quotes before approval. Current pricing seems high.");

        var pr5 = ProcurementRequest.Create(
            "Team Building Event",
            "Quarterly team outing — escape room and dinner",
            Department.HumanResources,
            Urgency.Low,
            RequesterId2);
        pr5.AddLineItem("Escape Room Booking (20 ppl)", 1, 500.00m);
        pr5.AddLineItem("Restaurant Reservation", 1, 1200.00m);

        await db.ProcurementRequests.AddRangeAsync(pr1, pr2, pr3, pr4, pr5);

        pr1.ClearDomainEvents();
        pr2.ClearDomainEvents();
        pr3.ClearDomainEvents();
        pr4.ClearDomainEvents();
        pr5.ClearDomainEvents();

        await db.SaveChangesAsync();
    }

    private static User CreateUser(Guid id, string name, string email, UserRole role, Department dept)
    {
        var user = User.Create(name, email, role, dept);
        var idProp = typeof(Entity).GetProperty("Id")!;
        idProp.SetValue(user, id);
        return user;
    }
}
