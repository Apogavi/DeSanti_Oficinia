namespace DeSanti.Application.DTOs.Usuarios;

public sealed record CriarUsuarioRequest(
    string UserName,
    string Name,
    string? Email,
    string Password,
    string Role);

public sealed record AtualizarUsuarioRequest(
    string? UserName,
    string Name,
    string? Email,
    string Role,
    bool IsActive);

public sealed record RedefinirSenhaRequest(string NewPassword);
