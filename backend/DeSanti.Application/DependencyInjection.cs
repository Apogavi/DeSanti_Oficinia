using DeSanti.Application.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DeSanti.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IClienteService, ClienteService>();

        return services;
    }
}