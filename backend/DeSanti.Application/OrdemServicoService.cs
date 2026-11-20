using DeSanti.Application.DTOs.OrdensServico;
using DeSanti.Application.Interfaces.Repositories;
using DeSanti.Application.Interfaces.Services;
using DeSanti.Application.Validators.OrdensServico;
using DeSanti.Domain;

namespace DeSanti.Application;

/// <summary>Coordena validation, consulta do cliente e persistência, sem depender de EF Core.</summary>
public sealed class OrdemServicoService(
    IClienteRepository clientes,
    IOrdemServicoRepository ordensServico) : IOrdemServicoService
{
    public async Task<ResultadoCriarOrdemServico> CriarAsync(
        CriarOrdemServicoRequest pedido,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        var erros = CriarOrdemServicoValidator.Validar(pedido);
        var chaveLimpa = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim();
        if (chaveLimpa is not null && chaveLimpa.Length > 64)
            erros["idempotencyKey"] = ["A chave de idempotência deve ter até 64 caracteres."];

        if (erros.Count > 0)
            return new(SituacaoCriacaoOrdemServico.DadosInvalidos, Erros: erros);

        if (chaveLimpa is not null)
        {
            var existente = await ordensServico.GetByIdempotencyKeyAsync(chaveLimpa, cancellationToken);
            if (existente is not null)
            {
                return new(SituacaoCriacaoOrdemServico.Criada,
                    new CriarOrdemServicoResponse(existente.Id, existente.TotalCentavos));
            }
        }

        var cliente = await clientes.GetByIdAsync(pedido.ClienteId);
        if (cliente is null)
            return new(SituacaoCriacaoOrdemServico.ClienteNaoEncontrado);
        if (!cliente.Ativo)
            return new(SituacaoCriacaoOrdemServico.ClienteInativo);

        // Os dados históricos vêm do cadastro persistido, nunca do request do cliente HTTP.
        var ordem = OrdemServico.Criar(
            cliente, pedido.Placa, pedido.Marca.Trim(), pedido.Modelo.Trim(), pedido.Cor.Trim(),
            pedido.Ano, pedido.DescricaoServico.Trim(),
            pedido.Pecas.Select(p => new PecaOrdemServico
            {
                Nome = p.Nome.Trim(),
                Quantidade = p.Quantidade,
                ValorTotalCentavos = p.ValorTotalCentavos,
                ValorUnitarioCentavos = p.ValorUnitarioCentavos,
                CustoUnitarioCentavos = p.CustoUnitarioCentavos
            }).ToList(),
            pedido.MaoDeObraCentavos, pedido.NecessitaRetorno, pedido.PrazoRetornoDias,
            pedido.EhOrcamento, chaveLimpa);

        await ordensServico.AddAsync(ordem, cancellationToken);
        return new(SituacaoCriacaoOrdemServico.Criada,
            new CriarOrdemServicoResponse(ordem.Id, ordem.TotalCentavos));
    }

    public async Task<ResultadoAprovarOrdemServico> AprovarOrcamentoAsync(
        long id, CancellationToken cancellationToken = default)
    {
        var ordem = await ordensServico.GetByIdAsync(id, cancellationToken);
        if (ordem is null)
            return new(SituacaoAprovacaoOrdemServico.NaoEncontrada);
        if (ordem.Status != StatusOrdemServico.Orcamento)
            return new(SituacaoAprovacaoOrdemServico.NaoEOrcamento);

        var atualizada = await ordensServico.SalvarAprovacaoAsync(id, cancellationToken);
        if (!atualizada)
            return new(SituacaoAprovacaoOrdemServico.NaoEOrcamento);

        if (ordem.Status == StatusOrdemServico.Orcamento)
            ordem.AprovarOrcamento();

        return new(SituacaoAprovacaoOrdemServico.Aprovada,
            new AprovarOrdemServicoResponse(ordem.Id, "aberta"));
    }

    public async Task<ResultadoFinalizarOrdemServico> FinalizarAsync(
        long id, CancellationToken cancellationToken = default)
    {
        var ordem = await ordensServico.GetByIdAsync(id, cancellationToken);
        if (ordem is null)
            return new(SituacaoFinalizacaoOrdemServico.NaoEncontrada);
        if (ordem.Status == StatusOrdemServico.Orcamento)
            return new(SituacaoFinalizacaoOrdemServico.OrcamentoNaoAprovado);
        if (ordem.DataConclusaoUtc.HasValue || ordem.Status == StatusOrdemServico.Concluida)
            return new(SituacaoFinalizacaoOrdemServico.JaConcluida);

        ordem.Finalizar(DateTime.UtcNow);
        var atualizada = await ordensServico.SalvarFinalizacaoAsync(ordem, cancellationToken);
        if (!atualizada)
            return new(SituacaoFinalizacaoOrdemServico.JaConcluida);

        return new(SituacaoFinalizacaoOrdemServico.Finalizada,
            new FinalizarOrdemServicoResponse(
                ordem.Id, ordem.DataConclusaoUtc!.Value, ordem.DataPrevistaRetornoUtc));
    }
}
