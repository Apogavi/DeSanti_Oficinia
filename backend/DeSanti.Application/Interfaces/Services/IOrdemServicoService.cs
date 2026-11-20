using DeSanti.Application.DTOs.OrdensServico;

namespace DeSanti.Application.Interfaces.Services;

public interface IOrdemServicoService
{
    Task<ResultadoCriarOrdemServico> CriarAsync(
        CriarOrdemServicoRequest pedido,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default);

    Task<ResultadoAprovarOrdemServico> AprovarOrcamentoAsync(
        long id, CancellationToken cancellationToken = default);

    Task<ResultadoFinalizarOrdemServico> FinalizarAsync(
        long id, CancellationToken cancellationToken = default);
}
