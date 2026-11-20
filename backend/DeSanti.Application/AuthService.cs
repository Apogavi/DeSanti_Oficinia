using System.Text.RegularExpressions;
using DeSanti.Application.DTOs.Auth;
using DeSanti.Application.Interfaces.Repositories;
using DeSanti.Application.Interfaces.Services;
using DeSanti.Application.Security;
using DeSanti.Domain;

namespace DeSanti.Application;

public sealed class AuthService(
    IUsuarioRepository usuarios,
    IJwtTokenGenerator jwtTokenGenerator) : IAuthService
{
    private static readonly Regex UserNameRegex = new(@"^[a-zA-Z0-9._@-]{3,64}$", RegexOptions.Compiled);

    public static string NormalizarUserName(string? userName) =>
        (userName ?? string.Empty).Trim().ToLowerInvariant();

    public static bool UserNameValido(string userName) =>
        UserNameRegex.IsMatch(userName);

    public static string? ValidarSenha(string? senha)
    {
        if (string.IsNullOrWhiteSpace(senha) || senha.Length < 6)
            return "A senha deve ter pelo menos 6 caracteres.";
        return null;
    }

    public async Task<(LoginResponse? Response, string? ErrorMessage)> LoginAsync(
        LoginRequest dto, CancellationToken ct = default)
    {
        var userName = NormalizarUserName(dto?.UserName);
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrEmpty(dto?.Password))
            return (null, "Usuário ou senha inválidos.");

        var usuario = await usuarios.GetByUserNameAsync(userName, ct);
        if (usuario is null)
            return (null, "Usuário ou senha inválidos.");

        // Valida a senha antes de verificar se está ativo para evitar enumeração de contas.
        if (!SenhaHasher.Verify(dto.Password, usuario.PasswordHash))
            return (null, "Usuário ou senha inválidos.");

        if (!usuario.Ativo)
            return (null, "Usuário desativado. Procure um administrador.");

        var (token, expiresAtUtc) = jwtTokenGenerator.Generate(usuario);
        return (new LoginResponse(token, expiresAtUtc, MapPerfil(usuario)), null);
    }

    public async Task<UsuarioPerfilDto?> GetProfileAsync(long userId, CancellationToken ct = default)
    {
        var usuario = await usuarios.GetByIdAsync(userId, ct);
        if (usuario is null || !usuario.Ativo)
            return null;

        return MapPerfil(usuario);
    }

    public async Task<(UsuarioPerfilDto? Profile, string? ErrorMessage)> UpdateProfileAsync(
        long userId, AtualizarMeuPerfilRequest dto, CancellationToken ct = default)
    {
        var usuario = await usuarios.GetByIdAsync(userId, ct);
        if (usuario is null || !usuario.Ativo)
            return (null, "Usuário não encontrado.");

        var novoUserName = NormalizarUserName(dto?.UserName);
        if (!UserNameValido(novoUserName))
            return (null, "Informe um nome de usuário válido (3 a 64 caracteres: letras, números, ponto, hífen ou underline).");

        var novoNome = (dto?.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(novoNome) || novoNome.Length > 120)
            return (null, "Informe o nome completo (até 120 caracteres).");

        var novoEmail = string.IsNullOrWhiteSpace(dto?.Email) ? null : dto.Email.Trim();
        if (novoEmail is not null && novoEmail.Length > 200)
            return (null, "O e-mail deve ter até 200 caracteres.");

        if (!string.Equals(usuario.NomeUsuario, novoUserName, StringComparison.OrdinalIgnoreCase))
        {
            if (await usuarios.ExisteComUserNameAsync(novoUserName, usuario.Id, ct))
                return (null, $"O nome de usuário '{novoUserName}' já está em uso.");

            usuario.NomeUsuario = novoUserName;
        }

        usuario.Nome = novoNome;
        usuario.Email = novoEmail;
        usuario.DataAlteracaoUtc = DateTime.UtcNow;

        await usuarios.UpdateAsync(usuario, ct);
        return (MapPerfil(usuario), null);
    }

    public async Task<(bool Success, string? ErrorMessage)> ChangePasswordAsync(
        long userId, AlterarSenhaRequest dto, CancellationToken ct = default)
    {
        var usuario = await usuarios.GetByIdAsync(userId, ct);
        if (usuario is null || !usuario.Ativo)
            return (false, "Usuário não encontrado.");

        if (string.IsNullOrEmpty(dto?.CurrentPassword) || !SenhaHasher.Verify(dto.CurrentPassword, usuario.PasswordHash))
            return (false, "A senha atual está incorreta.");

        var erroNovaSenha = ValidarSenha(dto.NewPassword);
        if (erroNovaSenha is not null)
            return (false, erroNovaSenha);

        usuario.PasswordHash = SenhaHasher.Hash(dto.NewPassword);
        usuario.DataAlteracaoUtc = DateTime.UtcNow;

        await usuarios.UpdateAsync(usuario, ct);
        return (true, null);
    }

    public static UsuarioPerfilDto MapPerfil(Usuario u) => new(
        u.Id,
        u.NomeUsuario,
        u.Nome,
        u.Email,
        u.Role,
        u.Ativo,
        u.DataCriacaoUtc);
}
