namespace DeSanti.Application.DTOs.OrdensServico;

public sealed record CriarOrdemServicoResponse(long Id, long TotalCentavos);

public enum SituacaoCriacaoOrdemServico
{
    Criada,
    DadosInvalidos,
    ClienteNaoEncontrado,
    ClienteInativo
}

/// <summary>Resultado do use case, independente de HTTP e do provider de banco.</summary>
public sealed record ResultadoCriarOrdemServico(
    SituacaoCriacaoOrdemServico Situacao,
    CriarOrdemServicoResponse? OrdemServico = null,
    Dictionary<string, string[]>? Erros = null);
