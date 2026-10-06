using DeSanti.Application.Interfaces.Repositories;
using DeSanti.Infrastructure.Data;
using DeSanti.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DeSanti.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<AppDbContext>(options =>
        {
            if (!string.IsNullOrEmpty(connectionString))
            {
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            }
        });

        services.AddScoped<IClienteRepository, ClienteRepository>();
        
        return services;
    }
}
