using DeSanti.Application.DTOs.Auth;
using DeSanti.Application.DTOs.Usuarios;
using DeSanti.Application.Interfaces.Services;
using DeSanti.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeSanti.API.Controllers;

[ApiController]
[Route("api/v1/usuarios")]
[Authorize(Roles = AppRoles.Admin)]
public sealed class UsuariosController(IUserManagementService userService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<List<UsuarioPerfilDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        return Ok(await userService.GetAllAsync(ct));
    }

    [HttpPost]
    [ProducesResponseType<UsuarioPerfilDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CriarUsuarioRequest dto, CancellationToken ct)
    {
        var (user, errorMessage) = await userService.CreateAsync(dto, ct);
        if (user is null)
            return BadRequest(new { message = errorMessage ?? "Não foi possível criar o usuário." });

        return StatusCode(StatusCodes.Status201Created, user);
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType<UsuarioPerfilDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update([FromRoute] long id, [FromBody] AtualizarUsuarioRequest dto, CancellationToken ct)
    {
        var (user, errorMessage) = await userService.UpdateAsync(id, dto, ct);
        if (user is null)
        {
            if (errorMessage == "Usuário não encontrado.")
                return NotFound(new { message = errorMessage });

            return BadRequest(new { message = errorMessage ?? "Não foi possível atualizar o usuário." });
        }

        return Ok(user);
    }

    [HttpPost("{id:long}/reset-password")]
    public async Task<IActionResult> ResetPassword([FromRoute] long id, [FromBody] RedefinirSenhaRequest dto, CancellationToken ct)
    {
        var (success, errorMessage) = await userService.ResetPasswordAsync(id, dto, ct);
        if (!success)
        {
            if (errorMessage == "Usuário não encontrado.")
                return NotFound(new { message = errorMessage });

            return BadRequest(new { message = errorMessage ?? "Não foi possível redefinir a senha." });
        }

        return NoContent();
    }
}
