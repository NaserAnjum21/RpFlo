using Microsoft.EntityFrameworkCore;
using RpFlo.Application.Interfaces;
using RpFlo.Domain.Entities;
using RpFlo.Domain.Enums;
using RpFlo.Infrastructure.Persistence;

namespace RpFlo.Infrastructure.Repositories;

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<IReadOnlyList<User>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        return await db.Users.AsNoTracking().Where(u => idList.Contains(u.Id)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default) =>
        await db.Users.AsNoTracking().OrderBy(u => u.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<User>> GetByRoleAsync(UserRole role, CancellationToken ct = default) =>
        await db.Users.AsNoTracking().Where(u => u.Role == role).ToListAsync(ct);
}
