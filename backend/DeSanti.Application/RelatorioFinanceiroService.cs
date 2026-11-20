using DeSanti.Application.DTOs.Relatorios;
using DeSanti.Application.Interfaces.Repositories;
using DeSanti.Domain;

namespace DeSanti.Application;

/// <summary>
/// Consolida métricas financeiras operacionais (semanal/mensal/personalizado) em centavos inteiros.
/// Mantém peças e mão de obra discriminadas e preserva custos históricos nulos sem tratá-los como zero.
/// </summary>
public sealed class RelatorioFinanceiroService(IConsultaOrdemRepository repository)
{
    public async Task<RelatorioFinanceiroResponse> GerarAsync(
        DateTime inicioUtc,
        DateTime fimUtc,
        string criterio,
        CancellationToken ct)
    {
        var porConclusao = criterio == "conclusao";
        var ordens = await repository.ListarParaRelatorioAsync(inicioUtc, fimUtc, porConclusao, ct);
        var orcamentosPendentes = await repository.ContarOrcamentosPendentesAsync(inicioUtc, fimUtc, ct);

        long brutoPecasComCusto = 0;
        long brutoPecasSemCusto = 0;
        long custoPecas = 0;
        long maoDeObra = 0;
        long totalCobrado = 0;
        int ordensComPecas = 0;
        int ordensSomenteMaoDeObra = 0;
        int ordensComCustoDesconhecido = 0;
        int pecasComCustoDesconhecido = 0;

        foreach (var ordem in ordens)
        {
            if (ordem.Status == StatusOrdemServico.Orcamento)
                continue;

            maoDeObra = checked(maoDeObra + ordem.MaoDeObraCentavos);
            totalCobrado = checked(totalCobrado + ordem.TotalCentavos);

            if (ordem.Pecas.Count > 0)
                ordensComPecas++;
            else
                ordensSomenteMaoDeObra++;

            var ordemTemCustoDesconhecido = false;
            foreach (var peca in ordem.Pecas)
            {
                var cobradoItem = peca.ValorTotalCentavos
                    ?? checked(peca.Quantidade * peca.ValorUnitarioCentavos);

                if (peca.CustoUnitarioCentavos is null)
                {
                    ordemTemCustoDesconhecido = true;
                    pecasComCustoDesconhecido++;
                    brutoPecasSemCusto = checked(brutoPecasSemCusto + cobradoItem);
                }
                else
                {
                    brutoPecasComCusto = checked(brutoPecasComCusto + cobradoItem);
                    custoPecas = checked(custoPecas + checked(peca.Quantidade * peca.CustoUnitarioCentavos.Value));
                }
            }

            if (ordemTemCustoDesconhecido)
                ordensComCustoDesconhecido++;
        }

        var qtdOrdens = ordensComPecas + ordensSomenteMaoDeObra;
        var brutoPecasTotal = checked(brutoPecasComCusto + brutoPecasSemCusto);
        var liquidoPecas = checked(brutoPecasComCusto - custoPecas);
        var liquidoOperacional = checked(liquidoPecas + maoDeObra);
        var ticketMedio = qtdOrdens > 0 ? totalCobrado / qtdOrdens : 0;

        return new RelatorioFinanceiroResponse(
            inicioUtc,
            fimUtc,
            criterio,
            qtdOrdens,
            ordensComPecas,
            ordensSomenteMaoDeObra,
            orcamentosPendentes,
            brutoPecasTotal,
            custoPecas,
            liquidoPecas,
            maoDeObra,
            liquidoOperacional,
            totalCobrado,
            ticketMedio,
            ordensComCustoDesconhecido,
            pecasComCustoDesconhecido,
            brutoPecasSemCusto);
    }
}
