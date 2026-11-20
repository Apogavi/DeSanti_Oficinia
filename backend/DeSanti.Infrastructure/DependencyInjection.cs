using DeSanti.Application.Interfaces.Repositories;
using DeSanti.Application.Interfaces.Services;
using DeSanti.Infrastructure.Data;
using DeSanti.Infrastructure.Identity;
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
                // O provider oficial suporta EF Core 10 e não consulta o banco no startup.
                options.UseMySQL(connectionString);
            }
        });

        services.Configure<JwtOptions>(opts =>
        {
            var section = configuration.GetSection(JwtOptions.SectionName);
            opts.SecretKey = section[nameof(JwtOptions.SecretKey)] ?? opts.SecretKey;
            opts.Issuer = section[nameof(JwtOptions.Issuer)] ?? opts.Issuer;
            opts.Audience = section[nameof(JwtOptions.Audience)] ?? opts.Audience;
            if (int.TryParse(section[nameof(JwtOptions.ExpirationHours)], out var hours))
                opts.ExpirationHours = hours;
        });
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IConsultaOrdemRepository, ConsultaOrdemRepository>();
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IOrdemServicoRepository, OrdemServicoRepository>();

        return services;
    }
}
