using DeSanti.Application.Interfaces.Repositories;
using DeSanti.Domain;
using DeSanti.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeSanti.Infrastructure.Repositories;

public sealed class UsuarioRepository(AppDbContext context) : IUsuarioRepository
{
    public Task<Usuario?> GetByIdAsync(long id, CancellationToken ct = default) =>
        context.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<Usuario?> GetByUserNameAsync(string nomeUsuarioNormalizado, CancellationToken ct = default) =>
        context.Usuarios.FirstOrDefaultAsync(u => u.NomeUsuario == nomeUsuarioNormalizado, ct);

    public Task<List<Usuario>> GetAllAsync(CancellationToken ct = default) =>
        context.Usuarios.AsNoTracking().ToListAsync(ct);

    public Task<bool> ExisteComUserNameAsync(string nomeUsuarioNormalizado, long? ignorarId = null, CancellationToken ct = default) =>
        context.Usuarios.AsNoTracking()
            .AnyAsync(u => u.NomeUsuario == nomeUsuarioNormalizado && (!ignorarId.HasValue || u.Id != ignorarId.Value), ct);

    public Task<int> ContarAdminsAtivosAsync(long? ignorarId = null, CancellationToken ct = default) =>
        context.Usuarios.AsNoTracking()
            .CountAsync(u => u.Ativo && u.Role == AppRoles.Admin && (!ignorarId.HasValue || u.Id != ignorarId.Value), ct);

    public Task<bool> AnyAsync(CancellationToken ct = default) =>
        context.Usuarios.AsNoTracking().AnyAsync(ct);

    public async Task AddAsync(Usuario usuario, CancellationToken ct = default)
    {
        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Usuario usuario, CancellationToken ct = default)
    {
        context.Usuarios.Update(usuario);
        await context.SaveChangesAsync(ct);
    }
}
