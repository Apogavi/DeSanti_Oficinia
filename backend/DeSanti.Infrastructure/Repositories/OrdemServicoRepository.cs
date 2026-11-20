using DeSanti.Application.Interfaces.Repositories;
using DeSanti.Domain;
using DeSanti.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeSanti.Infrastructure.Repositories;

public sealed class OrdemServicoRepository(AppDbContext context) : IOrdemServicoRepository
{
    public async Task AddAsync(OrdemServico ordemServico, CancellationToken cancellationToken = default)
    {
        // Uma transaction cobre o grafo inteiro: falhar em uma peça também desfaz a OS.
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        context.OrdemServico.Add(ordemServico);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public Task<OrdemServico?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        context.OrdemServico.AsNoTracking()
            .SingleOrDefaultAsync(os => os.Id == id, cancellationToken);

    public Task<OrdemServico?> GetByIdempotencyKeyAsync(
        string idempotencyKey, CancellationToken cancellationToken = default) =>
        context.OrdemServico.AsNoTracking()
            .SingleOrDefaultAsync(os => os.IdempotencyKey == idempotencyKey, cancellationToken);

    public async Task<bool> SalvarAprovacaoAsync(
        long id, CancellationToken cancellationToken = default)
    {
        var linhasAfetadas = await context.OrdemServico
            .Where(os => os.Id == id && os.Status == StatusOrdemServico.Orcamento)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(os => os.Status, StatusOrdemServico.Aberta),
                cancellationToken);

        return linhasAfetadas == 1;
    }

    public async Task<bool> SalvarFinalizacaoAsync(
        OrdemServico ordemServico, CancellationToken cancellationToken = default)
    {
        // Condição no banco garante atomicidade se duas requisições finalizarem a mesma OS.
        var linhasAfetadas = await context.OrdemServico
            .Where(os => os.Id == ordemServico.Id
                && os.Status == StatusOrdemServico.Aberta
                && os.DataConclusaoUtc == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(os => os.Status, StatusOrdemServico.Concluida)
                .SetProperty(os => os.DataConclusaoUtc, ordemServico.DataConclusaoUtc)
                .SetProperty(os => os.DataPrevistaRetornoUtc, ordemServico.DataPrevistaRetornoUtc),
                cancellationToken);

        return linhasAfetadas == 1;
    }
}
