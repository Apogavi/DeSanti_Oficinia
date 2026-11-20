using DeSanti.Application.DTOs.Auth;
using DeSanti.Application.DTOs.Usuarios;
using DeSanti.Application.Interfaces.Repositories;
using DeSanti.Application.Interfaces.Services;
using DeSanti.Application.Security;
using DeSanti.Domain;

namespace DeSanti.Application;

public sealed class UserManagementService(IUsuarioRepository usuarios) : IUserManagementService
{
    public async Task<List<UsuarioPerfilDto>> GetAllAsync(CancellationToken ct = default)
    {
        var lista = await usuarios.GetAllAsync(ct);
        return lista
            .OrderByDescending(u => u.Ativo)
            .ThenBy(u => u.Nome)
            .Select(AuthService.MapPerfil)
            .ToList();
    }

    public async Task<(UsuarioPerfilDto? User, string? ErrorMessage)> CreateAsync(
        CriarUsuarioRequest dto, CancellationToken ct = default)
    {
        var userName = AuthService.NormalizarUserName(dto?.UserName);
        if (!AuthService.UserNameValido(userName))
            return (null, "Informe um nome de usuário válido (3 a 64 caracteres: letras, números, ponto, hífen ou underline).");

        var nome = (dto?.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nome) || nome.Length > 120)
            return (null, "Informe o nome completo (até 120 caracteres).");

        var erroSenha = AuthService.ValidarSenha(dto?.Password);
        if (erroSenha is not null)
            return (null, erroSenha);

        if (!AppRoles.IsValid(dto?.Role))
            return (null, $"Perfil inválido. Use: {string.Join(", ", AppRoles.All)}.");

        var email = string.IsNullOrWhiteSpace(dto?.Email) ? null : dto.Email.Trim();
        if (email is not null && email.Length > 200)
            return (null, "O e-mail deve ter até 200 caracteres.");

        if (await usuarios.ExisteComUserNameAsync(userName, null, ct))
            return (null, $"O usuário '{userName}' já está cadastrado.");

        var novo = new Usuario
        {
            NomeUsuario = userName,
            Nome = nome,
            Email = email,
            PasswordHash = SenhaHasher.Hash(dto!.Password),
            Role = AppRoles.Normalize(dto.Role),
            Ativo = true,
            DataCriacaoUtc = DateTime.UtcNow
        };

        await usuarios.AddAsync(novo, ct);
        return (AuthService.MapPerfil(novo), null);
    }

    public async Task<(UsuarioPerfilDto? User, string? ErrorMessage)> UpdateAsync(
        long id, AtualizarUsuarioRequest dto, CancellationToken ct = default)
    {
        var usuario = await usuarios.GetByIdAsync(id, ct);
        if (usuario is null)
            return (null, "Usuário não encontrado.");

        if (!AppRoles.IsValid(dto?.Role))
            return (null, $"Perfil inválido. Use: {string.Join(", ", AppRoles.All)}.");

        var normalizedRole = AppRoles.Normalize(dto!.Role);
        var nome = (dto.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nome) || nome.Length > 120)
            return (null, "Informe o nome completo (até 120 caracteres).");

        var email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        if (email is not null && email.Length > 200)
            return (null, "O e-mail deve ter até 200 caracteres.");

        // Impede desativar ou rebaixar o último administrador ativo do sistema.
        var eraAdminAtivo = usuario.Ativo && string.Equals(usuario.Role, AppRoles.Admin, StringComparison.OrdinalIgnoreCase);
        var continuaraAdminAtivo = dto.IsActive && string.Equals(normalizedRole, AppRoles.Admin, StringComparison.OrdinalIgnoreCase);
        if (eraAdminAtivo && !continuaraAdminAtivo)
        {
            var outrosAdminsAtivos = await usuarios.ContarAdminsAtivosAsync(usuario.Id, ct);
            if (outrosAdminsAtivos == 0)
                return (null, "Não é possível desativar ou rebaixar o último administrador ativo do sistema.");
        }

        if (!string.IsNullOrWhiteSpace(dto.UserName))
        {
            var nextUserName = AuthService.NormalizarUserName(dto.UserName);
            if (!AuthService.UserNameValido(nextUserName))
                return (null, "Informe um nome de usuário válido (3 a 64 caracteres).");

            if (!string.Equals(usuario.NomeUsuario, nextUserName, StringComparison.OrdinalIgnoreCase))
            {
                if (await usuarios.ExisteComUserNameAsync(nextUserName, usuario.Id, ct))
                    return (null, $"O usuário '{nextUserName}' já está em uso.");

                usuario.NomeUsuario = nextUserName;
            }
        }

        usuario.Nome = nome;
        usuario.Email = email;
        usuario.Role = normalizedRole;
        usuario.Ativo = dto.IsActive;
        usuario.DataAlteracaoUtc = DateTime.UtcNow;

        await usuarios.UpdateAsync(usuario, ct);
        return (AuthService.MapPerfil(usuario), null);
    }

    public async Task<(bool Success, string? ErrorMessage)> ResetPasswordAsync(
        long id, RedefinirSenhaRequest dto, CancellationToken ct = default)
    {
        var usuario = await usuarios.GetByIdAsync(id, ct);
        if (usuario is null)
            return (false, "Usuário não encontrado.");

        var erroSenha = AuthService.ValidarSenha(dto?.NewPassword);
        if (erroSenha is not null)
            return (false, erroSenha);

        usuario.PasswordHash = SenhaHasher.Hash(dto!.NewPassword);
        usuario.DataAlteracaoUtc = DateTime.UtcNow;

        await usuarios.UpdateAsync(usuario, ct);
        return (true, null);
    }
}
