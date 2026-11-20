namespace DeSanti.Application.DTOs.Auth;

public sealed record LoginRequest(string UserName, string Password);

public sealed record UsuarioPerfilDto(
    long Id,
    string UserName,
    string Name,
    string? Email,
    string Role,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record LoginResponse(
    string Token,
    DateTime ExpiresAtUtc,
    UsuarioPerfilDto User);

public sealed record AtualizarMeuPerfilRequest(
    string UserName,
    string Name,
    string? Email);

public sealed record AlterarSenhaRequest(
    string CurrentPassword,
    string NewPassword);
