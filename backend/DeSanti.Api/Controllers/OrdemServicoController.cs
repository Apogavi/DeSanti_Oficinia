using DeSanti.Application.DTOs.OrdensServico;
using DeSanti.Application.Interfaces.Services;
using DeSanti.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeSanti.API.Controllers;

[ApiController]
[Route("api/v1/ordens-servico")]
[Authorize]
public sealed class OrdemServicoController(IOrdemServicoService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar([FromServices] DeSanti.Application.ConsultaOrdemService consulta,
        CancellationToken ct, [FromQuery] int pagina = 1, [FromQuery] string? busca = null, [FromQuery] string? status = null)
    {
        if (pagina < 1 || pagina > 100000 || busca?.Length > 200 ||
            status is not (null or "orcamento" or "aberta" or "concluida"))
            return BadRequest();

        StatusOrdemServico? filtroStatus = status switch
        {
            "orcamento" => StatusOrdemServico.Orcamento,
            "aberta" => StatusOrdemServico.Aberta,
            "concluida" => StatusOrdemServico.Concluida,
            _ => null
        };
        var resultado = await consulta.ListarPaginadoAsync(pagina, busca, filtroStatus, ct);
        Response.Headers["X-Has-Next-Page"] = resultado.TemProxima ? "true" : "false";
        return Ok(resultado.Itens);
    }

    [HttpGet("retornos")]
    [ProducesResponseType<List<RetornoOrdemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ListarRetornos(
        [FromServices] DeSanti.Application.ConsultaOrdemService consulta,
        CancellationToken ct,
        [FromQuery] int dias = 14)
    {
        if (dias < 1 || dias > 365)
            return BadRequest();

        return Ok(await consulta.ListarRetornosAsync(dias, ct));
    }

    [HttpGet("veiculo/{placa}")]
    [ProducesResponseType<HistoricoVeiculoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterHistoricoVeiculo(
        string placa,
        [FromServices] DeSanti.Application.ConsultaOrdemService consulta,
        CancellationToken ct)
    {
        if (DeSanti.Application.ConsultaOrdemService.NormalizarPlacaValida(placa) is null)
            return BadRequest();

        var historico = await consulta.ObterHistoricoVeiculoAsync(placa, ct);
        return historico is null ? NotFound() : Ok(historico);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Obter(long id, [FromServices] DeSanti.Application.ConsultaOrdemService consulta,
        CancellationToken ct)
    {
        var ordem = await consulta.ObterAsync(id, ct);
        return ordem is null ? NotFound() : Ok(ordem);
    }

    [HttpPost("{id:long}/aprovar")]
    [ProducesResponseType<AprovarOrdemServicoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AprovarAsync(long id, CancellationToken cancellationToken)
    {
        var resultado = await service.AprovarOrcamentoAsync(id, cancellationToken);

        return resultado.Situacao switch
        {
            SituacaoAprovacaoOrdemServico.Aprovada => Ok(resultado.OrdemServico),
            SituacaoAprovacaoOrdemServico.NaoEncontrada => Problem(
                statusCode: StatusCodes.Status404NotFound, title: "Ordem de serviço não encontrada."),
            SituacaoAprovacaoOrdemServico.NaoEOrcamento => Problem(
                statusCode: StatusCodes.Status409Conflict, title: "Apenas orçamentos podem ser aprovados."),
            _ => throw new InvalidOperationException("Resultado de aprovação desconhecido.")
        };
    }

    [HttpPost("{id:long}/finalizar")]
    [ProducesResponseType<FinalizarOrdemServicoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> FinalizarAsync(long id, CancellationToken cancellationToken)
    {
        var resultado = await service.FinalizarAsync(id, cancellationToken);

        return resultado.Situacao switch
        {
            SituacaoFinalizacaoOrdemServico.Finalizada => Ok(resultado.OrdemServico),
            SituacaoFinalizacaoOrdemServico.NaoEncontrada => Problem(
                statusCode: StatusCodes.Status404NotFound, title: "Ordem de serviço não encontrada."),
            SituacaoFinalizacaoOrdemServico.OrcamentoNaoAprovado => Problem(
                statusCode: StatusCodes.Status409Conflict, title: "Aprove o orçamento antes de finalizar a ordem de serviço."),
            SituacaoFinalizacaoOrdemServico.JaConcluida => Problem(
                statusCode: StatusCodes.Status409Conflict, title: "A ordem de serviço já foi finalizada."),
            _ => throw new InvalidOperationException("Resultado de finalização desconhecido.")
        };
    }

    [HttpPost]
    [ProducesResponseType<CriarOrdemServicoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CriarAsync(
        [FromBody] CriarOrdemServicoRequest pedido,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        var resultado = await service.CriarAsync(pedido, idempotencyKey, cancellationToken);

        // A Application descreve o resultado de negócio; só a Api escolhe o HTTP status.
        return resultado.Situacao switch
        {
            SituacaoCriacaoOrdemServico.Criada => StatusCode(StatusCodes.Status201Created, resultado.OrdemServico),
            SituacaoCriacaoOrdemServico.DadosInvalidos => BadRequest(new ValidationProblemDetails(resultado.Erros!)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Confira os dados da ordem de serviço."
            }),
            SituacaoCriacaoOrdemServico.ClienteNaoEncontrado => Problem(
                statusCode: StatusCodes.Status404NotFound, title: "Cliente não encontrado."),
            SituacaoCriacaoOrdemServico.ClienteInativo => Problem(
                statusCode: StatusCodes.Status409Conflict, title: "O cliente está inativo."),
            _ => throw new InvalidOperationException("Resultado de criação desconhecido.")
        };
    }
}
