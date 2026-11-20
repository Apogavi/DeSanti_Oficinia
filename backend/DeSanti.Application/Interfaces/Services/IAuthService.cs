using DeSanti.Application.DTOs.Auth;

namespace DeSanti.Application.Interfaces.Services;

public interface IAuthService
{
    Task<(LoginResponse? Response, string? ErrorMessage)> LoginAsync(LoginRequest dto, CancellationToken ct = default);
    Task<UsuarioPerfilDto?> GetProfileAsync(long userId, CancellationToken ct = default);
    Task<(UsuarioPerfilDto? Profile, string? ErrorMessage)> UpdateProfileAsync(long userId, AtualizarMeuPerfilRequest dto, CancellationToken ct = default);
    Task<(bool Success, string? ErrorMessage)> ChangePasswordAsync(long userId, AlterarSenhaRequest dto, CancellationToken ct = default);
}
