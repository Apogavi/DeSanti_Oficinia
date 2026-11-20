using DeSanti.Application.DTOs.Auth;
using DeSanti.Application.DTOs.Usuarios;

namespace DeSanti.Application.Interfaces.Services;

public interface IUserManagementService
{
    Task<List<UsuarioPerfilDto>> GetAllAsync(CancellationToken ct = default);
    Task<(UsuarioPerfilDto? User, string? ErrorMessage)> CreateAsync(CriarUsuarioRequest dto, CancellationToken ct = default);
    Task<(UsuarioPerfilDto? User, string? ErrorMessage)> UpdateAsync(long id, AtualizarUsuarioRequest dto, CancellationToken ct = default);
    Task<(bool Success, string? ErrorMessage)> ResetPasswordAsync(long id, RedefinirSenhaRequest dto, CancellationToken ct = default);
}
