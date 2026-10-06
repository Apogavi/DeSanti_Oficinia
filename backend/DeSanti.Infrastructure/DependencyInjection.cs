using DeSanti.Application.Interfaces;
using DeSanti.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DeSanti.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IDbConnectionFactory>(
            _ => new MySqlConnectionFactory(configuration.GetConnectionString("DefaultConnection")));

        return services;
    }
}
