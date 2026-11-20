using DeSanti.Application;
using DeSanti.Application.DTOs.Relatorios;
using DeSanti.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeSanti.API.Controllers;

[ApiController]
[Route("api/v1/relatorios/financeiro")]
[Authorize(Roles = AppRoles.Admin)]
public sealed class RelatorioFinanceiroController(RelatorioFinanceiroService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<RelatorioFinanceiroResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ObterAsync(
        [FromQuery] DateTime? inicioUtc = null,
        [FromQuery] DateTime? fimUtc = null,
        [FromQuery] string criterio = "conclusao",
        CancellationToken ct = default)
    {
        if (criterio is not ("conclusao" or "abertura"))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Critério inválido. Use 'conclusao' ou 'abertura'.");
        }

        var agora = DateTime.UtcNow;
        var fim = fimUtc.HasValue
            ? DateTime.SpecifyKind(fimUtc.Value.ToUniversalTime(), DateTimeKind.Utc)
            : agora;
        var inicio = inicioUtc.HasValue
            ? DateTime.SpecifyKind(inicioUtc.Value.ToUniversalTime(), DateTimeKind.Utc)
            : new DateTime(agora.Year, agora.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        if (inicio > fim)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "A data inicial deve ser anterior ou igual à data final.");
        }

        if ((fim - inicio).TotalDays > 366)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "O intervalo máximo de consulta é de 366 dias.");
        }

        var resultado = await service.GerarAsync(inicio, fim, criterio, ct);
        return Ok(resultado);
    }
}
