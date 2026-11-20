using System.Reflection;
using DeSanti.Domain;
using Microsoft.EntityFrameworkCore;

namespace DeSanti.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<OrdemServico> OrdemServico => Set<OrdemServico>();
    public DbSet<PecaOrdemServico> PecasOrdemServico => Set<PecaOrdemServico>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<KeepAwakeRecord> KeepAwakeRecords => Set<KeepAwakeRecord>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}