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

        var now = DateTimeOffset.UtcNow;
        var requests = new List<ProcurementRequest>();

        // --- PO Issued (4) - oldest, fully processed ---

        var pr1 = CreateRequest("Office Supplies Restock", "Monthly restocking of office supplies for all departments",
            Department.Operations, Urgency.Low, RequesterId1, now.AddDays(-55));
        pr1.AddLineItem("Copy Paper (Case)", 20, 45.00m);
        pr1.AddLineItem("Ink Cartridges", 10, 32.99m);
        pr1.AddLineItem("Sticky Notes", 50, 3.99m);
        Submit(pr1, RequesterId1, now.AddDays(-54));
        ApproveManager(pr1, ManagerId, now.AddDays(-53), "Standard monthly order");
        ApproveFinance(pr1, FinanceId, now.AddDays(-52), "Within monthly office budget");
        IssuePo(pr1, FinanceId, now.AddDays(-51));
        requests.Add(pr1);

        var pr2 = CreateRequest("Ergonomic Keyboards", "Mechanical keyboards for developer team to reduce RSI risk",
            Department.Engineering, Urgency.Medium, RequesterId1, now.AddDays(-48));
        pr2.AddLineItem("Kinesis Advantage360", 8, 449.00m);
        pr2.AddLineItem("Keyboard Wrist Rest", 8, 24.99m);
        Submit(pr2, RequesterId1, now.AddDays(-47));
        ApproveManager(pr2, ManagerId, now.AddDays(-45), "Health & safety priority");
        ApproveFinance(pr2, FinanceId, now.AddDays(-43));
        IssuePo(pr2, FinanceId, now.AddDays(-42));
        requests.Add(pr2);

        var pr3 = CreateRequest("Conference Room AV Upgrade", "Replace aging projectors with modern displays and webcams",
            Department.Operations, Urgency.High, RequesterId1, now.AddDays(-40));
        pr3.AddLineItem("75\" 4K Display", 3, 1899.00m);
        pr3.AddLineItem("Conference Webcam", 3, 299.99m);
        pr3.AddLineItem("Wireless Presenter", 3, 79.99m);
        pr3.AddLineItem("HDMI Cables (10ft)", 6, 12.99m);
        Submit(pr3, RequesterId1, now.AddDays(-39));
        ApproveManager(pr3, ManagerId, now.AddDays(-37));
        ApproveFinance(pr3, FinanceId, now.AddDays(-34), "Capital expenditure approved for Q2");
        IssuePo(pr3, FinanceId, now.AddDays(-33));
        requests.Add(pr3);

        var pr4 = CreateRequest("Marketing Print Materials", "Brochures and business cards for trade show next month",
            Department.Marketing, Urgency.High, RequesterId2, now.AddDays(-35));
        pr4.AddLineItem("Brochures (1000 pack)", 5, 350.00m);
        pr4.AddLineItem("Business Cards (500)", 10, 45.00m);
        pr4.AddLineItem("Banner Stands", 3, 275.00m);
        Submit(pr4, RequesterId2, now.AddDays(-34));
        ApproveManager(pr4, ManagerId, now.AddDays(-33), "Trade show budget approved");
        ApproveFinance(pr4, FinanceId, now.AddDays(-31));
        IssuePo(pr4, FinanceId, now.AddDays(-30));
        requests.Add(pr4);

        // --- Finance Approved (2) - awaiting PO issuance ---

        var pr5 = CreateRequest("Development Laptops", "New MacBook Pro laptops for engineering team expansion",
            Department.Engineering, Urgency.High, RequesterId1, now.AddDays(-20));
        pr5.AddLineItem("MacBook Pro 16\" M4", 5, 2499.00m);
        pr5.AddLineItem("USB-C Dock", 5, 189.99m);
        pr5.AddLineItem("Monitor Stand", 5, 49.99m);
        Submit(pr5, RequesterId1, now.AddDays(-19));
        ApproveManager(pr5, ManagerId, now.AddDays(-17), "Team expansion confirmed by CTO");
        ApproveFinance(pr5, FinanceId, now.AddDays(-14), "CapEx budget available");
        requests.Add(pr5);

        var pr6 = CreateRequest("Safety Equipment Renewal", "Annual safety equipment replacement for warehouse staff",
            Department.Operations, Urgency.Critical, RequesterId1, now.AddDays(-18));
        pr6.AddLineItem("Hard Hats", 25, 32.00m);
        pr6.AddLineItem("Safety Goggles", 25, 18.50m);
        pr6.AddLineItem("Hi-Vis Vests", 25, 14.99m);
        pr6.AddLineItem("Steel-Toe Boots", 25, 89.99m);
        Submit(pr6, RequesterId1, now.AddDays(-17));
        ApproveManager(pr6, ManagerId, now.AddDays(-16), "Compliance requirement");
        ApproveFinance(pr6, FinanceId, now.AddDays(-14));
        requests.Add(pr6);

        // --- Manager Approved (3) - awaiting finance review ---

        var pr7 = CreateRequest("Marketing Campaign Materials", "Branded merchandise for Q3 campaign launch",
            Department.Marketing, Urgency.Medium, RequesterId2, now.AddDays(-12));
        pr7.AddLineItem("Branded T-Shirts", 200, 12.50m);
        pr7.AddLineItem("Tote Bags", 150, 8.75m);
        pr7.AddLineItem("Stickers (Sheet of 50)", 100, 3.25m);
        Submit(pr7, RequesterId2, now.AddDays(-11));
        ApproveManager(pr7, ManagerId, now.AddDays(-9), "Approved for Q3 campaign budget");
        requests.Add(pr7);

        var pr8 = CreateRequest("Cloud Infrastructure Expansion", "Additional AWS capacity for new microservices",
            Department.Engineering, Urgency.High, RequesterId1, now.AddDays(-10));
        pr8.AddLineItem("Reserved EC2 Instances (1yr)", 4, 3200.00m);
        pr8.AddLineItem("RDS Instance Upgrade", 2, 1800.00m);
        pr8.AddLineItem("CloudWatch Advanced Monitoring", 1, 450.00m);
        Submit(pr8, RequesterId1, now.AddDays(-9));
        ApproveManager(pr8, ManagerId, now.AddDays(-7));
        requests.Add(pr8);

        var pr9 = CreateRequest("HR Onboarding Kits", "Welcome kits for 15 new hires starting next month",
            Department.HumanResources, Urgency.Medium, RequesterId2, now.AddDays(-8));
        pr9.AddLineItem("Welcome Package Box", 15, 35.00m);
        pr9.AddLineItem("Company Notebook", 15, 12.00m);
        pr9.AddLineItem("Branded Water Bottle", 15, 18.00m);
        pr9.AddLineItem("Lanyard + Badge Holder", 15, 5.50m);
        Submit(pr9, RequesterId2, now.AddDays(-7));
        ApproveManager(pr9, ManagerId, now.AddDays(-5), "New hire batch confirmed");
        requests.Add(pr9);

        // --- Submitted (3) - awaiting manager approval ---

        var pr10 = CreateRequest("Sales Team Tablets", "iPads for field sales team demos and order entry",
            Department.Sales, Urgency.Medium, RequesterId2, now.AddDays(-6));
        pr10.AddLineItem("iPad Pro 11\"", 8, 1099.00m);
        pr10.AddLineItem("Apple Pencil", 8, 129.00m);
        pr10.AddLineItem("Protective Case", 8, 49.99m);
        Submit(pr10, RequesterId2, now.AddDays(-5));
        requests.Add(pr10);

        var pr11 = CreateRequest("Server Upgrade", "Upgrade production database server hardware",
            Department.Engineering, Urgency.Critical, RequesterId1, now.AddDays(-4));
        pr11.AddLineItem("Dell PowerEdge R760", 1, 12500.00m);
        pr11.AddLineItem("128GB DDR5 ECC RAM", 4, 899.00m);
        pr11.AddLineItem("NVMe SSD 4TB", 4, 649.00m);
        Submit(pr11, RequesterId1, now.AddDays(-3));
        requests.Add(pr11);

        var pr12 = CreateRequest("Office Coffee Service", "Premium coffee service subscription for break rooms",
            Department.Operations, Urgency.Low, RequesterId2, now.AddDays(-2));
        pr12.AddLineItem("Coffee Machine Lease (Monthly)", 3, 150.00m);
        pr12.AddLineItem("Premium Coffee Beans (Monthly)", 3, 85.00m);
        pr12.AddLineItem("Supplies Kit", 3, 45.00m);
        Submit(pr12, RequesterId2, now.AddDays(-1));
        requests.Add(pr12);

        // --- Manager Rejected (2) ---

        var pr13 = CreateRequest("Standing Desk Converters", "Desktop standing desk risers for interested team members",
            Department.Engineering, Urgency.Low, RequesterId1, now.AddDays(-30));
        pr13.AddLineItem("VariDesk Pro Plus 36", 12, 395.00m);
        pr13.AddLineItem("Anti-Fatigue Mat", 12, 49.99m);
        Submit(pr13, RequesterId1, now.AddDays(-29));
        RejectManager(pr13, ManagerId, now.AddDays(-27), "Need updated vendor quotes before approval. Current pricing seems high compared to recent catalog.");
        requests.Add(pr13);

        var pr14 = CreateRequest("Team Retreat Venue", "Booking for annual engineering team retreat",
            Department.Engineering, Urgency.Medium, RequesterId1, now.AddDays(-22));
        pr14.AddLineItem("Venue Rental (3 days)", 1, 4500.00m);
        pr14.AddLineItem("Catering Package", 30, 85.00m);
        pr14.AddLineItem("AV Equipment Rental", 1, 750.00m);
        Submit(pr14, RequesterId1, now.AddDays(-21));
        RejectManager(pr14, ManagerId, now.AddDays(-19), "Budget freeze on discretionary spending until Q3 review completes. Resubmit after July 15th.");
        requests.Add(pr14);

        // --- Finance Rejected (2) ---

        var pr15 = CreateRequest("Premium Design Software Licenses", "Adobe Creative Cloud and Figma enterprise licenses",
            Department.Marketing, Urgency.High, RequesterId2, now.AddDays(-25));
        pr15.AddLineItem("Adobe Creative Cloud (Annual)", 10, 659.88m);
        pr15.AddLineItem("Figma Enterprise (Annual)", 10, 540.00m);
        Submit(pr15, RequesterId2, now.AddDays(-24));
        ApproveManager(pr15, ManagerId, now.AddDays(-22), "Essential tooling for design team");
        RejectFinance(pr15, FinanceId, now.AddDays(-19), "Annual license costs exceed department budget. Please explore volume discount options and resubmit with revised pricing.");
        requests.Add(pr15);

        var pr16 = CreateRequest("Office Furniture Refresh", "Replace worn conference room furniture",
            Department.Operations, Urgency.Low, RequesterId1, now.AddDays(-28));
        pr16.AddLineItem("Conference Table (12-seat)", 2, 2800.00m);
        pr16.AddLineItem("Ergonomic Chair", 24, 450.00m);
        pr16.AddLineItem("Whiteboard (6ft)", 4, 320.00m);
        Submit(pr16, RequesterId1, now.AddDays(-27));
        ApproveManager(pr16, ManagerId, now.AddDays(-25));
        RejectFinance(pr16, FinanceId, now.AddDays(-22), "Total exceeds furniture capex threshold. Split into two requests — chairs this quarter, tables next quarter.");
        requests.Add(pr16);

        // --- Drafts (4) ---

        var pr17 = CreateRequest("Team Building Event", "Quarterly team outing — escape room and dinner",
            Department.HumanResources, Urgency.Low, RequesterId2, now.AddDays(-3));
        pr17.AddLineItem("Escape Room Booking (20 ppl)", 1, 500.00m);
        pr17.AddLineItem("Restaurant Reservation", 1, 1200.00m);
        requests.Add(pr17);

        var pr18 = CreateRequest("Developer Conference Tickets", "Tickets and travel for team to attend re:Invent",
            Department.Engineering, Urgency.Medium, RequesterId1, now.AddDays(-2));
        pr18.AddLineItem("Conference Pass", 4, 1799.00m);
        pr18.AddLineItem("Flight (Round Trip)", 4, 650.00m);
        pr18.AddLineItem("Hotel (4 nights)", 4, 280.00m);
        requests.Add(pr18);

        var pr19 = CreateRequest("Warehouse Shelving Units", "Additional storage for inventory overflow",
            Department.Operations, Urgency.High, RequesterId2, now.AddDays(-1));
        pr19.AddLineItem("Heavy-Duty Shelving Unit", 10, 189.00m);
        pr19.AddLineItem("Shelf Labels (100 pack)", 5, 15.99m);
        requests.Add(pr19);

        var pr20 = CreateRequest("Customer Support Headsets", "Wireless headsets for support team",
            Department.Sales, Urgency.Medium, RequesterId1, now.AddDays(-1));
        pr20.AddLineItem("Jabra Evolve2 75", 12, 299.99m);
        pr20.AddLineItem("Headset Stand", 12, 24.99m);
        requests.Add(pr20);

        await db.ProcurementRequests.AddRangeAsync(requests);

        foreach (var r in requests)
            r.ClearDomainEvents();

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
