using DeSanti.Domain;

namespace DeSanti.Application.Interfaces.Repositories;

public interface IOrdemServicoRepository
{
    /// <summary>Persiste a OS e suas peças como uma única operação atômica.</summary>
    Task AddAsync(OrdemServico ordemServico, CancellationToken cancellationToken = default);

    Task<OrdemServico?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<OrdemServico?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    /// <summary>Promove um orçamento para OS em aberto de forma atômica.</summary>
    Task<bool> SalvarAprovacaoAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Atualiza conclusão e retorno apenas enquanto a OS ainda estiver em aberto no banco.</summary>
    Task<bool> SalvarFinalizacaoAsync(OrdemServico ordemServico, CancellationToken cancellationToken = default);
}
