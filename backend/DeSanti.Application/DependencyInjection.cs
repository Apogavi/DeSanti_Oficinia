using DeSanti.Application.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DeSanti.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registra os Application Services com um escopo por request HTTP,
    /// compatível com os Repositories e o DbContext da Infrastructure.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ConsultaOrdemService>();
        services.AddScoped<RelatorioFinanceiroService>();
        services.AddScoped<KeepAwakeService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IClienteService, ClienteService>();
        services.AddScoped<IOrdemServicoService, OrdemServicoService>();

        return services;
    }
}
