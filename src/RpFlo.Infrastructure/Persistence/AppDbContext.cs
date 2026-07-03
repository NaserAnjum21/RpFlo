using Microsoft.EntityFrameworkCore;
using RpFlo.Domain.Common;
using RpFlo.Domain.Entities;

namespace RpFlo.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{

    public DbSet<ProcurementRequest> ProcurementRequests => Set<ProcurementRequest>();
    public DbSet<LineItem> LineItems => Set<LineItem>();
    public DbSet<User> Users => Set<User>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Role).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Department).HasConversion<string>().HasMaxLength(50);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Role);
        });

        modelBuilder.Entity<ProcurementRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.Department).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Urgency).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.PoNumber).HasMaxLength(50);

            entity.Property(e => e.RowVersion).IsRowVersion();

            entity.HasMany(e => e.LineItems)
                .WithOne()
                .HasForeignKey(li => li.ProcurementRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.AuditEntries)
                .WithOne()
                .HasForeignKey(a => a.ProcurementRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Comments)
                .WithOne()
                .HasForeignKey(c => c.ProcurementRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.RequesterId);
            entity.HasIndex(e => e.Department);

            entity.Ignore(e => e.TotalAmount);
            entity.Ignore(e => e.DomainEvents);

            entity.ToTable(tb => tb.IsTemporal());
        });

        modelBuilder.Entity<LineItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.OwnsOne(e => e.UnitPrice, money =>
            {
                money.Property(m => m.Amount).HasColumnName("UnitPrice").HasPrecision(18, 2);
                money.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3).HasDefaultValue("USD");
                money.ToTable("LineItems", tb => tb.IsTemporal(t =>
                {
                    t.HasPeriodStart("PeriodStart").HasColumnName("PeriodStart");
                    t.HasPeriodEnd("PeriodEnd").HasColumnName("PeriodEnd");
                }));
            });
            entity.Ignore(e => e.TotalPrice);
            entity.Ignore(e => e.DomainEvents);

            entity.ToTable("LineItems", tb => tb.IsTemporal(t =>
            {
                t.HasPeriodStart("PeriodStart").HasColumnName("PeriodStart");
                t.HasPeriodEnd("PeriodEnd").HasColumnName("PeriodEnd");
            }));
        });

        modelBuilder.Entity<AuditEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).HasMaxLength(100).IsRequired();
            entity.Property(e => e.FromStatus).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.ToStatus).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.Comment).HasMaxLength(1000);
            entity.HasIndex(e => e.ProcurementRequestId);
            entity.Ignore(e => e.DomainEvents);
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Text).HasMaxLength(2000).IsRequired();
            entity.HasIndex(e => e.ProcurementRequestId);
            entity.Ignore(e => e.DomainEvents);

            entity.ToTable(tb => tb.IsTemporal());
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Message).HasMaxLength(1000).IsRequired();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.IsRead });
            entity.Ignore(e => e.DomainEvents);

            entity.ToTable(tb => tb.IsTemporal());
        });
    }
}
