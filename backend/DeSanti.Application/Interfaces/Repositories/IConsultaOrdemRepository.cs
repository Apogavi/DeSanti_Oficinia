using DeSanti.Domain;

namespace DeSanti.Application.Interfaces.Repositories;

public interface IConsultaOrdemRepository
{
    Task<List<OrdemServico>> ListarAsync(int pagina, string? busca, StatusOrdemServico? status, CancellationToken ct);
    Task<OrdemServico?> ObterAsync(long id, CancellationToken ct);
    Task<List<OrdemServico>> ListarRetornosAsync(DateTime limiteUtc, int limiteItens, CancellationToken ct);
    Task<List<OrdemServico>> ListarPorPlacaAsync(string placaNormalizada, int limiteItens, CancellationToken ct);
    Task<List<OrdemServico>> ListarParaRelatorioAsync(DateTime inicioUtc, DateTime fimUtc, bool porConclusao, CancellationToken ct);
    Task<int> ContarOrcamentosPendentesAsync(DateTime inicioUtc, DateTime fimUtc, CancellationToken ct);
}
