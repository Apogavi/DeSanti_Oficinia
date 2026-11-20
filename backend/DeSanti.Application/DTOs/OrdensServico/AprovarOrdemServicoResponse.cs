namespace DeSanti.Application.DTOs.OrdensServico;

public sealed record AprovarOrdemServicoResponse(long Id, string Status);

public enum SituacaoAprovacaoOrdemServico
{
    Aprovada,
    NaoEncontrada,
    NaoEOrcamento
}

public sealed record ResultadoAprovarOrdemServico(
    SituacaoAprovacaoOrdemServico Situacao,
    AprovarOrdemServicoResponse? OrdemServico = null);
