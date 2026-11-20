namespace DeSanti.Application.DTOs.OrdensServico;

public record ConsultaPecaResponse(
    string Nome,
    int Quantidade,
    long ValorUnitarioCentavos,
    long? CustoUnitarioCentavos,
    long? ValorTotalCentavos);

public record ConsultaOrdemResponse(
    long Id,
    string NomeCliente,
    string DocumentoCliente,
    string TelefoneCliente,
    string Placa,
    string Marca,
    string Modelo,
    string Cor,
    int? Ano,
    string DescricaoServico,
    long MaoDeObraCentavos,
    long TotalCentavos,
    DateTime DataCriacaoUtc,
    DateTime? DataConclusaoUtc,
    DateTime? DataPrevistaRetornoUtc,
    bool NecessitaRetorno,
    int? PrazoRetornoDias,
    List<ConsultaPecaResponse> Pecas,
    string Status = "aberta");

public record PaginaConsultaOrdemResponse(
    List<ConsultaOrdemResponse> Itens,
    bool TemProxima);

public record RetornoOrdemResponse(
    long Id,
    string NomeCliente,
    string TelefoneCliente,
    string Placa,
    string Marca,
    string Modelo,
    DateTime DataConclusaoUtc,
    DateTime DataPrevistaRetornoUtc,
    int? PrazoRetornoDias,
    string DescricaoServico);

public record HistoricoVeiculoResponse(
    string Placa,
    string Marca,
    string Modelo,
    string Cor,
    int? Ano,
    long UltimoClienteId,
    string UltimoNomeCliente,
    List<ConsultaOrdemResponse> UltimasOrdens);
