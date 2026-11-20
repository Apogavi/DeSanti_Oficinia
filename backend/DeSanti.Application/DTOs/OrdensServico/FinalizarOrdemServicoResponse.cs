namespace DeSanti.Application.DTOs.OrdensServico;

public sealed record FinalizarOrdemServicoResponse(
    long Id,
    DateTime DataConclusaoUtc,
    DateTime? DataPrevistaRetornoUtc);

public enum SituacaoFinalizacaoOrdemServico
{
    Finalizada,
    NaoEncontrada,
    JaConcluida,
    OrcamentoNaoAprovado
}

/// <summary>Resultado da finalização da OS, independente de HTTP e do provider de banco.</summary>
public sealed record ResultadoFinalizarOrdemServico(
    SituacaoFinalizacaoOrdemServico Situacao,
    FinalizarOrdemServicoResponse? OrdemServico = null);
