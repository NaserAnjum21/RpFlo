using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RpFlo.Application.Interfaces;
using RpFlo.Infrastructure.Persistence;
using RpFlo.Infrastructure.Repositories;

namespace RpFlo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IProcurementRepository, ProcurementRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
