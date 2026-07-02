using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using RpFlo.Application.Services;

namespace RpFlo.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<ProcurementService>();
        services.AddScoped<ProcurementService>();
        return services;
    }
}
