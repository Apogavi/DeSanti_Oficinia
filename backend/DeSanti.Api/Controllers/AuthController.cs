using System.Security.Claims;
using DeSanti.Application.DTOs.Auth;
using DeSanti.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeSanti.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Login([FromBody] LoginRequest dto, CancellationToken ct)
    {
        var (response, errorMessage) = await authService.LoginAsync(dto, ct);
        if (response is null)
            return Unauthorized(new { message = errorMessage ?? "Usuário ou senha inválidos." });

        return Ok(response);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<UsuarioPerfilDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        if (!TryObterUserId(out var userId))
            return Unauthorized(new { message = "Sessão inválida." });

        var profile = await authService.GetProfileAsync(userId, ct);
        if (profile is null)
            return Unauthorized(new { message = "Usuário inativo ou não encontrado." });

        return Ok(profile);
    }

    [HttpPut("me")]
    [Authorize]
    [ProducesResponseType<UsuarioPerfilDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateMe([FromBody] AtualizarMeuPerfilRequest dto, CancellationToken ct)
    {
        if (!TryObterUserId(out var userId))
            return Unauthorized(new { message = "Sessão inválida." });

        var (profile, errorMessage) = await authService.UpdateProfileAsync(userId, dto, ct);
        if (profile is null)
            return BadRequest(new { message = errorMessage ?? "Não foi possível atualizar o perfil." });

        return Ok(profile);
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] AlterarSenhaRequest dto, CancellationToken ct)
    {
        if (!TryObterUserId(out var userId))
            return Unauthorized(new { message = "Sessão inválida." });

        var (success, errorMessage) = await authService.ChangePasswordAsync(userId, dto, ct);
        if (!success)
            return BadRequest(new { message = errorMessage ?? "Não foi possível alterar a senha." });

        return NoContent();
    }

    private bool TryObterUserId(out long userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(raw, out userId) && userId > 0;
    }
}
