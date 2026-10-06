namespace DeSanti.Application.DTOs.OrdensServico;

public sealed record PecaRequest(
    string Nome,
    int Quantidade,
    long ValorUnitarioCentavos
);

public sealed record CriarOrdemServicoRequest(
    string NomeCliente,
    string Cpf,
    string Telefone,
    string Placa,
    string Marca,
    string Modelo,
    string Cor,
    int? Ano,
    string DescricaoServico,
    List<PecaRequest> Pecas,
    long MaoDeObraCentavos,
    bool NecessitaRetorno,
    int? PrazoRetornoDias
);
