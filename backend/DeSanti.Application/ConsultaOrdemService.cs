using System.Text.RegularExpressions;
using DeSanti.Application.DTOs.OrdensServico;
using DeSanti.Application.Interfaces.Repositories;
using DeSanti.Domain;

namespace DeSanti.Application;

public sealed class ConsultaOrdemService(IConsultaOrdemRepository repository)
{
    public const int TamanhoPagina = 20;
    private static readonly Regex PlacaRegex = new(@"^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$", RegexOptions.Compiled);

    public async Task<List<ConsultaOrdemResponse>> ListarAsync(int pagina, string? busca, StatusOrdemServico? status, CancellationToken ct) =>
        (await ListarPaginadoAsync(pagina, busca, status, ct)).Itens;

    public async Task<PaginaConsultaOrdemResponse> ListarPaginadoAsync(
        int pagina, string? busca, StatusOrdemServico? status, CancellationToken ct)
    {
        var registros = await repository.ListarAsync(pagina, busca, status, ct);
        var temProxima = registros.Count > TamanhoPagina;
        var itens = registros.Take(TamanhoPagina).Select(Map).ToList();
        return new PaginaConsultaOrdemResponse(itens, temProxima);
    }

    public async Task<ConsultaOrdemResponse?> ObterAsync(long id, CancellationToken ct)
    {
        var ordem = await repository.ObterAsync(id, ct);
        return ordem is null ? null : Map(ordem);
    }

    public async Task<List<RetornoOrdemResponse>> ListarRetornosAsync(int diasAdiante, CancellationToken ct)
    {
        var limiteUtc = DateTime.UtcNow.AddDays(diasAdiante);
        var ordens = await repository.ListarRetornosAsync(limiteUtc, 50, ct);
        return ordens
            .Where(o => o.DataConclusaoUtc.HasValue && o.DataPrevistaRetornoUtc.HasValue)
            .Select(o => new RetornoOrdemResponse(
                o.Id,
                o.NomeCliente,
                o.TelefoneCliente,
                o.Placa,
                o.Marca,
                o.Modelo,
                o.DataConclusaoUtc!.Value,
                o.DataPrevistaRetornoUtc!.Value,
                o.PrazoRetornoDias,
                o.DescricaoServico))
            .ToList();
    }

    public static string? NormalizarPlacaValida(string? placa)
    {
        if (string.IsNullOrWhiteSpace(placa)) return null;
        var normalizada = placa.Replace("-", "").Replace(" ", "").Trim().ToUpperInvariant();
        return PlacaRegex.IsMatch(normalizada) ? normalizada : null;
    }

    public async Task<HistoricoVeiculoResponse?> ObterHistoricoVeiculoAsync(string placa, CancellationToken ct)
    {
        var normalizada = NormalizarPlacaValida(placa);
        if (normalizada is null) return null;

        var ordens = await repository.ListarPorPlacaAsync(normalizada, 10, ct);
        if (ordens.Count == 0) return null;

        var recente = ordens[0];
        return new HistoricoVeiculoResponse(
            recente.Placa,
            recente.Marca,
            recente.Modelo,
            recente.Cor,
            recente.Ano,
            recente.ClienteId,
            recente.NomeCliente,
            ordens.Select(Map).ToList());
    }

    private static string StatusParaTexto(OrdemServico o)
    {
        if (o.Status == StatusOrdemServico.Orcamento) return "orcamento";
        if (o.Status == StatusOrdemServico.Concluida || o.DataConclusaoUtc.HasValue) return "concluida";
        return "aberta";
    }

    private static ConsultaOrdemResponse Map(OrdemServico o) => new(
        o.Id, o.NomeCliente, o.DocumentoCliente, o.TelefoneCliente,
        o.Placa, o.Marca, o.Modelo, o.Cor, o.Ano, o.DescricaoServico,
        o.MaoDeObraCentavos, o.TotalCentavos, o.DataCriacaoUtc,
        o.DataConclusaoUtc, o.DataPrevistaRetornoUtc, o.NecessitaRetorno, o.PrazoRetornoDias,
        o.Pecas.Select(p => new ConsultaPecaResponse(
            p.Nome, p.Quantidade, p.ValorUnitarioCentavos, p.CustoUnitarioCentavos, p.ValorTotalCentavos)).ToList(),
        StatusParaTexto(o));
}
