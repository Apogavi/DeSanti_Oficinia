using DeSanti.Application.Interfaces.Repositories;
using DeSanti.Domain;
using DeSanti.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace DeSanti.Infrastructure.Repositories;
public sealed class ConsultaOrdemRepository(AppDbContext db) : IConsultaOrdemRepository {
    public Task<List<OrdemServico>> ListarAsync(int pagina, string? busca, StatusOrdemServico? status, CancellationToken ct)
    {
        var consulta = db.OrdemServico.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            var placa = termo.Replace("-", "").Replace(" ", "").ToUpperInvariant();
            var documento = termo.Any(char.IsLetter)
                ? ""
                : new string(termo.Where(char.IsAsciiDigit).ToArray());
            consulta = consulta.Where(os => os.NomeCliente.Contains(termo) ||
                (placa != "" && os.Placa.Contains(placa)) ||
                (documento != "" && os.DocumentoCliente.Contains(documento)));
        }

        if (status.HasValue)
        {
            consulta = status.Value switch
            {
                StatusOrdemServico.Orcamento => consulta.Where(os => os.Status == StatusOrdemServico.Orcamento),
                StatusOrdemServico.Concluida => consulta.Where(os =>
                    os.Status == StatusOrdemServico.Concluida || os.DataConclusaoUtc != null),
                _ => consulta.Where(os =>
                    os.Status == StatusOrdemServico.Aberta && os.DataConclusaoUtc == null)
            };
        }

        return consulta.OrderByDescending(os => os.DataCriacaoUtc).ThenByDescending(os => os.Id)
            .Skip((pagina - 1) * 20).Take(21).ToListAsync(ct);
    }

    public Task<OrdemServico?> ObterAsync(long id, CancellationToken ct) =>
        db.OrdemServico.AsNoTracking().Include(o => o.Pecas).SingleOrDefaultAsync(o => o.Id == id, ct);

    public Task<List<OrdemServico>> ListarRetornosAsync(DateTime limiteUtc, int limiteItens, CancellationToken ct) =>
        db.OrdemServico.AsNoTracking()
            .Where(os => os.DataConclusaoUtc != null
                && os.NecessitaRetorno
                && os.DataPrevistaRetornoUtc != null
                && os.DataPrevistaRetornoUtc <= limiteUtc)
            .OrderBy(os => os.DataPrevistaRetornoUtc)
            .ThenByDescending(os => os.Id)
            .Take(limiteItens)
            .ToListAsync(ct);

    public Task<List<OrdemServico>> ListarPorPlacaAsync(string placaNormalizada, int limiteItens, CancellationToken ct) =>
        db.OrdemServico.AsNoTracking()
            .Where(os => os.Placa == placaNormalizada)
            .OrderByDescending(os => os.DataCriacaoUtc)
            .ThenByDescending(os => os.Id)
            .Take(limiteItens)
            .ToListAsync(ct);

    public Task<List<OrdemServico>> ListarParaRelatorioAsync(
        DateTime inicioUtc, DateTime fimUtc, bool porConclusao, CancellationToken ct)
    {
        var consulta = db.OrdemServico.AsNoTracking().Include(o => o.Pecas).AsQueryable();
        consulta = porConclusao
            ? consulta.Where(os =>
                os.Status != StatusOrdemServico.Orcamento
                && os.DataConclusaoUtc != null
                && os.DataConclusaoUtc >= inicioUtc
                && os.DataConclusaoUtc <= fimUtc)
            : consulta.Where(os =>
                os.Status != StatusOrdemServico.Orcamento
                && os.DataCriacaoUtc >= inicioUtc
                && os.DataCriacaoUtc <= fimUtc);

        return consulta.ToListAsync(ct);
    }

    public Task<int> ContarOrcamentosPendentesAsync(DateTime inicioUtc, DateTime fimUtc, CancellationToken ct) =>
        db.OrdemServico.AsNoTracking()
            .CountAsync(os =>
                os.Status == StatusOrdemServico.Orcamento
                && os.DataCriacaoUtc >= inicioUtc
                && os.DataCriacaoUtc <= fimUtc,
                ct);
}
