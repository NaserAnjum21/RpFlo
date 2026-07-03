using RpFlo.Application.Interfaces;
using RpFlo.Infrastructure.Persistence;

namespace RpFlo.Infrastructure.Repositories;

public sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
