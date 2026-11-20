namespace DeSanti.Application.DTOs.Relatorios;

/// <summary>
/// Resumo financeiro operacional da oficina para um período.
/// Mantém peças e mão de obra discriminadas e nunca interpreta custo histórico nulo como zero.
/// </summary>
public sealed record RelatorioFinanceiroResponse(
    DateTime InicioUtc,
    DateTime FimUtc,
    string Criterio,
    int QuantidadeOrdens,
    int QuantidadeOrdensComPecas,
    int QuantidadeOrdensSomenteMaoDeObra,
    int QuantidadeOrcamentosPendentes,
    long BrutoPecasCentavos,
    long CustoPecasCentavos,
    long LiquidoPecasCentavos,
    long MaoDeObraCentavos,
    long LiquidoOperacionalCentavos,
    long TotalCobradoCentavos,
    long TicketMedioCentavos,
    int QuantidadeOrdensComCustoDesconhecido,
    int QuantidadePecasComCustoDesconhecido,
    long BrutoPecasSemCustoCentavos);
