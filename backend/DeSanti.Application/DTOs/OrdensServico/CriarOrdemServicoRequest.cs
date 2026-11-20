namespace DeSanti.Application.DTOs.OrdensServico;

/// <summary>
/// Item de peça. <c>CustoUnitarioCentavos</c> é obrigatório desde 10/11/2026; o tipo
/// permanece nullable para que um JSON sem o campo gere erro de validation por campo
/// (<c>400</c>) em vez de ser interpretado silenciosamente como zero.
/// </summary>
public sealed record PecaRequest(
    string Nome,
    int Quantidade,
    long ValorUnitarioCentavos,
    long? CustoUnitarioCentavos,
    long? ValorTotalCentavos = null
);

/// <summary>
/// API contract de criação: referencia um cliente existente e recebe centavos inteiros.
/// Total, dados históricos do cliente e datas oficiais são definidos pelo backend.
/// </summary>
public sealed record CriarOrdemServicoRequest(
    long ClienteId,
    string Placa,
    string Marca,
    string Modelo,
    string Cor,
    int? Ano,
    string DescricaoServico,
    List<PecaRequest> Pecas,
    long MaoDeObraCentavos,
    bool NecessitaRetorno,
    int? PrazoRetornoDias,
    bool EhOrcamento = false
);
