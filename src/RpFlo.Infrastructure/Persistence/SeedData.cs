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

        if (await db.Users.AnyAsync())
        {
            return;
        }

        var users = new[]
        {
            CreateUser(RequesterId1, name: "Alice Johnson", email: "alice@company.com", UserRole.Requester, Department.Engineering),
            CreateUser(RequesterId2, name: "Bob Smith", email: "bob@company.com", UserRole.Requester, Department.Marketing),
            CreateUser(ManagerId, name: "Carol Williams", email: "carol@company.com", UserRole.Manager, Department.Operations),
            CreateUser(FinanceId, name: "Dave Brown", email: "dave@company.com", UserRole.Finance, Department.Finance),
            CreateUser(AdminId, name: "Eve Davis", email: "eve@company.com", UserRole.Admin, Department.Operations),
        };

        await db.Users.AddRangeAsync(users);

        var now = DateTimeOffset.UtcNow;
        var requests = new List<ProcurementRequest>();

        // --- PO Issued (4) - oldest, fully processed ---

        var pr1 = CreateRequest(
            title: "Office Supplies Restock",
            description: "Monthly restocking of office supplies for all departments",
            Department.Operations, Urgency.Low, RequesterId1, now.AddDays(-55));

        pr1.AddLineItem(name: "Copy Paper (Case)", quantity: 20, unitPrice: 45.00m);
        pr1.AddLineItem(name: "Ink Cartridges", quantity: 10, unitPrice: 32.99m);
        pr1.AddLineItem(name: "Sticky Notes", quantity: 50, unitPrice: 3.99m);
        Submit(pr1, RequesterId1, now.AddDays(-54));
        ApproveManager(pr1, ManagerId, now.AddDays(-53), comment: "Standard monthly order");
        ApproveFinance(pr1, FinanceId, now.AddDays(-52), comment: "Within monthly office budget");
        IssuePo(pr1, FinanceId, now.AddDays(-51));
        requests.Add(pr1);

        var pr2 = CreateRequest(
            title: "Ergonomic Keyboards",
            description: "Mechanical keyboards for developer team to reduce RSI risk",
            Department.Engineering, Urgency.Medium, RequesterId1, now.AddDays(-48));

        pr2.AddLineItem(name: "Kinesis Advantage360", quantity: 8, unitPrice: 449.00m);
        pr2.AddLineItem(name: "Keyboard Wrist Rest", quantity: 8, unitPrice: 24.99m);
        Submit(pr2, RequesterId1, now.AddDays(-47));
        ApproveManager(pr2, ManagerId, now.AddDays(-45), comment: "Health & safety priority");
        ApproveFinance(pr2, FinanceId, now.AddDays(-43));
        IssuePo(pr2, FinanceId, now.AddDays(-42));
        requests.Add(pr2);

        var pr3 = CreateRequest(
            title: "Conference Room AV Upgrade",
            description: "Replace aging projectors with modern displays and webcams",
            Department.Operations, Urgency.High, RequesterId1, now.AddDays(-40));

        pr3.AddLineItem(name: "75\" 4K Display", quantity: 3, unitPrice: 1899.00m);
        pr3.AddLineItem(name: "Conference Webcam", quantity: 3, unitPrice: 299.99m);
        pr3.AddLineItem(name: "Wireless Presenter", quantity: 3, unitPrice: 79.99m);
        pr3.AddLineItem(name: "HDMI Cables (10ft)", quantity: 6, unitPrice: 12.99m);
        Submit(pr3, RequesterId1, now.AddDays(-39));
        ApproveManager(pr3, ManagerId, now.AddDays(-37));
        ApproveFinance(pr3, FinanceId, now.AddDays(-34), comment: "Capital expenditure approved for Q2");
        IssuePo(pr3, FinanceId, now.AddDays(-33));
        requests.Add(pr3);

        var pr4 = CreateRequest(
            title: "Marketing Print Materials",
            description: "Brochures and business cards for trade show next month",
            Department.Marketing, Urgency.High, RequesterId2, now.AddDays(-35));

        pr4.AddLineItem(name: "Brochures (1000 pack)", quantity: 5, unitPrice: 350.00m);
        pr4.AddLineItem(name: "Business Cards (500)", quantity: 10, unitPrice: 45.00m);
        pr4.AddLineItem(name: "Banner Stands", quantity: 3, unitPrice: 275.00m);
        Submit(pr4, RequesterId2, now.AddDays(-34));
        ApproveManager(pr4, ManagerId, now.AddDays(-33), comment: "Trade show budget approved");
        ApproveFinance(pr4, FinanceId, now.AddDays(-31));
        IssuePo(pr4, FinanceId, now.AddDays(-30));
        requests.Add(pr4);

        // --- Finance Approved (2) - awaiting PO issuance ---

        var pr5 = CreateRequest(
            title: "Development Laptops",
            description: "New MacBook Pro laptops for engineering team expansion",
            Department.Engineering, Urgency.High, RequesterId1, now.AddDays(-20));

        pr5.AddLineItem(name: "MacBook Pro 16\" M4", quantity: 5, unitPrice: 2499.00m);
        pr5.AddLineItem(name: "USB-C Dock", quantity: 5, unitPrice: 189.99m);
        pr5.AddLineItem(name: "Monitor Stand", quantity: 5, unitPrice: 49.99m);
        Submit(pr5, RequesterId1, now.AddDays(-19));
        ApproveManager(pr5, ManagerId, now.AddDays(-17), comment: "Team expansion confirmed by CTO");
        ApproveFinance(pr5, FinanceId, now.AddDays(-14), comment: "CapEx budget available");
        requests.Add(pr5);

        var pr6 = CreateRequest(
            title: "Safety Equipment Renewal",
            description: "Annual safety equipment replacement for warehouse staff",
            Department.Operations, Urgency.Critical, RequesterId1, now.AddDays(-18));

        pr6.AddLineItem(name: "Hard Hats", quantity: 25, unitPrice: 32.00m);
        pr6.AddLineItem(name: "Safety Goggles", quantity: 25, unitPrice: 18.50m);
        pr6.AddLineItem(name: "Hi-Vis Vests", quantity: 25, unitPrice: 14.99m);
        pr6.AddLineItem(name: "Steel-Toe Boots", quantity: 25, unitPrice: 89.99m);
        Submit(pr6, RequesterId1, now.AddDays(-17));
        ApproveManager(pr6, ManagerId, now.AddDays(-16), comment: "Compliance requirement");
        ApproveFinance(pr6, FinanceId, now.AddDays(-14));
        requests.Add(pr6);

        // --- Manager Approved (3) - awaiting finance review ---

        var pr7 = CreateRequest(
            title: "Marketing Campaign Materials",
            description: "Branded merchandise for Q3 campaign launch",
            Department.Marketing, Urgency.Medium, RequesterId2, now.AddDays(-12));

        pr7.AddLineItem(name: "Branded T-Shirts", quantity: 200, unitPrice: 12.50m);
        pr7.AddLineItem(name: "Tote Bags", quantity: 150, unitPrice: 8.75m);
        pr7.AddLineItem(name: "Stickers (Sheet of 50)", quantity: 100, unitPrice: 3.25m);
        Submit(pr7, RequesterId2, now.AddDays(-11));
        ApproveManager(pr7, ManagerId, now.AddDays(-9), comment: "Approved for Q3 campaign budget");
        requests.Add(pr7);

        var pr8 = CreateRequest(
            title: "Cloud Infrastructure Expansion",
            description: "Additional AWS capacity for new microservices",
            Department.Engineering, Urgency.High, RequesterId1, now.AddDays(-10));

        pr8.AddLineItem(name: "Reserved EC2 Instances (1yr)", quantity: 4, unitPrice: 3200.00m);
        pr8.AddLineItem(name: "RDS Instance Upgrade", quantity: 2, unitPrice: 1800.00m);
        pr8.AddLineItem(name: "CloudWatch Advanced Monitoring", quantity: 1, unitPrice: 450.00m);
        Submit(pr8, RequesterId1, now.AddDays(-9));
        ApproveManager(pr8, ManagerId, now.AddDays(-7));
        requests.Add(pr8);

        var pr9 = CreateRequest(
            title: "HR Onboarding Kits",
            description: "Welcome kits for 15 new hires starting next month",
            Department.HumanResources, Urgency.Medium, RequesterId2, now.AddDays(-8));

        pr9.AddLineItem(name: "Welcome Package Box", quantity: 15, unitPrice: 35.00m);
        pr9.AddLineItem(name: "Company Notebook", quantity: 15, unitPrice: 12.00m);
        pr9.AddLineItem(name: "Branded Water Bottle", quantity: 15, unitPrice: 18.00m);
        pr9.AddLineItem(name: "Lanyard + Badge Holder", quantity: 15, unitPrice: 5.50m);
        Submit(pr9, RequesterId2, now.AddDays(-7));
        ApproveManager(pr9, ManagerId, now.AddDays(-5), comment: "New hire batch confirmed");
        requests.Add(pr9);

        // --- Submitted (3) - awaiting manager approval ---

        var pr10 = CreateRequest(
            title: "Sales Team Tablets",
            description: "iPads for field sales team demos and order entry",
            Department.Sales, Urgency.Medium, RequesterId2, now.AddDays(-6));

        pr10.AddLineItem(name: "iPad Pro 11\"", quantity: 8, unitPrice: 1099.00m);
        pr10.AddLineItem(name: "Apple Pencil", quantity: 8, unitPrice: 129.00m);
        pr10.AddLineItem(name: "Protective Case", quantity: 8, unitPrice: 49.99m);
        Submit(pr10, RequesterId2, now.AddDays(-5));
        requests.Add(pr10);

        var pr11 = CreateRequest(
            title: "Server Upgrade",
            description: "Upgrade production database server hardware",
            Department.Engineering, Urgency.Critical, RequesterId1, now.AddDays(-4));

        pr11.AddLineItem(name: "Dell PowerEdge R760", quantity: 1, unitPrice: 12500.00m);
        pr11.AddLineItem(name: "128GB DDR5 ECC RAM", quantity: 4, unitPrice: 899.00m);
        pr11.AddLineItem(name: "NVMe SSD 4TB", quantity: 4, unitPrice: 649.00m);
        Submit(pr11, RequesterId1, now.AddDays(-3));
        requests.Add(pr11);

        var pr12 = CreateRequest(
            title: "Office Coffee Service",
            description: "Premium coffee service subscription for break rooms",
            Department.Operations, Urgency.Low, RequesterId2, now.AddDays(-2));

        pr12.AddLineItem(name: "Coffee Machine Lease (Monthly)", quantity: 3, unitPrice: 150.00m);
        pr12.AddLineItem(name: "Premium Coffee Beans (Monthly)", quantity: 3, unitPrice: 85.00m);
        pr12.AddLineItem(name: "Supplies Kit", quantity: 3, unitPrice: 45.00m);
        Submit(pr12, RequesterId2, now.AddDays(-1));
        requests.Add(pr12);

        // --- Manager Rejected (2) ---

        var pr13 = CreateRequest(
            title: "Standing Desk Converters",
            description: "Desktop standing desk risers for interested team members",
            Department.Engineering, Urgency.Low, RequesterId1, now.AddDays(-30));

        pr13.AddLineItem(name: "VariDesk Pro Plus 36", quantity: 12, unitPrice: 395.00m);
        pr13.AddLineItem(name: "Anti-Fatigue Mat", quantity: 12, unitPrice: 49.99m);
        Submit(pr13, RequesterId1, now.AddDays(-29));

        RejectManager(pr13, ManagerId, now.AddDays(-27),
            reason: "Need updated vendor quotes before approval. Current pricing seems high compared to recent catalog.");

        requests.Add(pr13);

        var pr14 = CreateRequest(
            title: "Team Retreat Venue",
            description: "Booking for annual engineering team retreat",
            Department.Engineering, Urgency.Medium, RequesterId1, now.AddDays(-22));

        pr14.AddLineItem(name: "Venue Rental (3 days)", quantity: 1, unitPrice: 4500.00m);
        pr14.AddLineItem(name: "Catering Package", quantity: 30, unitPrice: 85.00m);
        pr14.AddLineItem(name: "AV Equipment Rental", quantity: 1, unitPrice: 750.00m);
        Submit(pr14, RequesterId1, now.AddDays(-21));

        RejectManager(pr14, ManagerId, now.AddDays(-19),
            reason: "Budget freeze on discretionary spending until Q3 review completes. Resubmit after July 15th.");

        requests.Add(pr14);

        // --- Finance Rejected (2) ---

        var pr15 = CreateRequest(
            title: "Premium Design Software Licenses",
            description: "Adobe Creative Cloud and Figma enterprise licenses",
            Department.Marketing, Urgency.High, RequesterId2, now.AddDays(-25));

        pr15.AddLineItem(name: "Adobe Creative Cloud (Annual)", quantity: 10, unitPrice: 659.88m);
        pr15.AddLineItem(name: "Figma Enterprise (Annual)", quantity: 10, unitPrice: 540.00m);
        Submit(pr15, RequesterId2, now.AddDays(-24));
        ApproveManager(pr15, ManagerId, now.AddDays(-22), comment: "Essential tooling for design team");

        RejectFinance(pr15, FinanceId, now.AddDays(-19),
            reason: "Annual license costs exceed department budget. Please explore volume discount options and resubmit with revised pricing.");

        requests.Add(pr15);

        var pr16 = CreateRequest(
            title: "Office Furniture Refresh",
            description: "Replace worn conference room furniture",
            Department.Operations, Urgency.Low, RequesterId1, now.AddDays(-28));

        pr16.AddLineItem(name: "Conference Table (12-seat)", quantity: 2, unitPrice: 2800.00m);
        pr16.AddLineItem(name: "Ergonomic Chair", quantity: 24, unitPrice: 450.00m);
        pr16.AddLineItem(name: "Whiteboard (6ft)", quantity: 4, unitPrice: 320.00m);
        Submit(pr16, RequesterId1, now.AddDays(-27));
        ApproveManager(pr16, ManagerId, now.AddDays(-25));

        RejectFinance(pr16, FinanceId, now.AddDays(-22),
            reason: "Total exceeds furniture capex threshold. Split into two requests — chairs this quarter, tables next quarter.");

        requests.Add(pr16);

        // --- Drafts (4) ---

        var pr17 = CreateRequest(
            title: "Team Building Event",
            description: "Quarterly team outing — escape room and dinner",
            Department.HumanResources, Urgency.Low, RequesterId2, now.AddDays(-3));

        pr17.AddLineItem(name: "Escape Room Booking (20 ppl)", quantity: 1, unitPrice: 500.00m);
        pr17.AddLineItem(name: "Restaurant Reservation", quantity: 1, unitPrice: 1200.00m);
        requests.Add(pr17);

        var pr18 = CreateRequest(
            title: "Developer Conference Tickets",
            description: "Tickets and travel for team to attend re:Invent",
            Department.Engineering, Urgency.Medium, RequesterId1, now.AddDays(-2));

        pr18.AddLineItem(name: "Conference Pass", quantity: 4, unitPrice: 1799.00m);
        pr18.AddLineItem(name: "Flight (Round Trip)", quantity: 4, unitPrice: 650.00m);
        pr18.AddLineItem(name: "Hotel (4 nights)", quantity: 4, unitPrice: 280.00m);
        requests.Add(pr18);

        var pr19 = CreateRequest(
            title: "Warehouse Shelving Units",
            description: "Additional storage for inventory overflow",
            Department.Operations, Urgency.High, RequesterId2, now.AddDays(-1));

        pr19.AddLineItem(name: "Heavy-Duty Shelving Unit", quantity: 10, unitPrice: 189.00m);
        pr19.AddLineItem(name: "Shelf Labels (100 pack)", quantity: 5, unitPrice: 15.99m);
        requests.Add(pr19);

        var pr20 = CreateRequest(
            title: "Customer Support Headsets",
            description: "Wireless headsets for support team",
            Department.Sales, Urgency.Medium, RequesterId1, now.AddDays(-1));

        pr20.AddLineItem(name: "Jabra Evolve2 75", quantity: 12, unitPrice: 299.99m);
        pr20.AddLineItem(name: "Headset Stand", quantity: 12, unitPrice: 24.99m);
        requests.Add(pr20);

        await db.ProcurementRequests.AddRangeAsync(requests);

        foreach (var r in requests)
        {
            r.ClearDomainEvents();
        }

        await db.SaveChangesAsync();
    }

    private static ProcurementRequest CreateRequest(
        string title, string description, Department dept, Urgency urgency, Guid requesterId, DateTimeOffset createdAt)
    {
        var pr = ProcurementRequest.Create(title, description, dept, urgency, requesterId);
        SetCreatedAt(pr, createdAt);
        return pr;
    }

    private static void Submit(ProcurementRequest pr, Guid requesterId, DateTimeOffset at)
    {
        pr.Submit(requesterId);
        SetUpdatedAt(pr, at);
        BackdateLastAudit(pr, at);
    }

    private static void ApproveManager(ProcurementRequest pr, Guid managerId, DateTimeOffset at, string? comment = null)
    {
        pr.ApproveByManager(managerId, comment);
        SetUpdatedAt(pr, at);
        BackdateLastAudit(pr, at);
    }

    private static void RejectManager(ProcurementRequest pr, Guid managerId, DateTimeOffset at, string reason)
    {
        pr.RejectByManager(managerId, reason);
        SetUpdatedAt(pr, at);
        BackdateLastAudit(pr, at);
    }

    private static void ApproveFinance(ProcurementRequest pr, Guid financeId, DateTimeOffset at, string? comment = null)
    {
        pr.ApproveByFinance(financeId, comment);
        SetUpdatedAt(pr, at);
        BackdateLastAudit(pr, at);
    }

    private static void RejectFinance(ProcurementRequest pr, Guid financeId, DateTimeOffset at, string reason)
    {
        pr.RejectByFinance(financeId, reason);
        SetUpdatedAt(pr, at);
        BackdateLastAudit(pr, at);
    }

    private static void IssuePo(ProcurementRequest pr, Guid financeId, DateTimeOffset at)
    {
        pr.IssuePurchaseOrder(financeId);
        SetUpdatedAt(pr, at);
        BackdateLastAudit(pr, at);
    }

    private static void SetCreatedAt(Entity entity, DateTimeOffset date)
    {
        typeof(Entity).GetProperty("CreatedAt")!.SetValue(entity, date);
        typeof(Entity).GetProperty("UpdatedAt")!.SetValue(entity, date);
    }

    private static void SetUpdatedAt(Entity entity, DateTimeOffset date)
    {
        typeof(Entity).GetProperty("UpdatedAt")!.SetValue(entity, date);
    }

    private static void BackdateLastAudit(ProcurementRequest pr, DateTimeOffset date)
    {
        var lastAudit = pr.AuditEntries[^1];
        SetCreatedAt(lastAudit, date);
    }

    private static User CreateUser(Guid id, string name, string email, UserRole role, Department dept)
    {
        var user = User.Create(name, email, role, dept);
        typeof(Entity).GetProperty("Id")!.SetValue(user, id);
        return user;
    }
}
