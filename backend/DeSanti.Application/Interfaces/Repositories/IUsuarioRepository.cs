using DeSanti.Domain;

namespace DeSanti.Application.Interfaces.Repositories;

public interface IUsuarioRepository
{
    Task<Usuario?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<Usuario?> GetByUserNameAsync(string nomeUsuarioNormalizado, CancellationToken ct = default);
    Task<List<Usuario>> GetAllAsync(CancellationToken ct = default);
    Task<bool> ExisteComUserNameAsync(string nomeUsuarioNormalizado, long? ignorarId = null, CancellationToken ct = default);
    Task<int> ContarAdminsAtivosAsync(long? ignorarId = null, CancellationToken ct = default);
    Task<bool> AnyAsync(CancellationToken ct = default);
    Task AddAsync(Usuario usuario, CancellationToken ct = default);
    Task UpdateAsync(Usuario usuario, CancellationToken ct = default);
}
