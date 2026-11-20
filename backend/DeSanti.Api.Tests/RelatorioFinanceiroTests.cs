using System.Net;
using System.Net.Http.Json;
using DeSanti.Api.Tests.Integration;
using DeSanti.Application;
using DeSanti.Application.DTOs.Relatorios;
using DeSanti.Application.Interfaces.Repositories;
using DeSanti.Domain;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DeSanti.Api.Tests;

public class RelatorioFinanceiroTests
{
    [Fact]
    public async Task CalculaBrutoCustoLiquidoPecasMaoDeObraESinalizaHistoricoSemCusto()
    {
        var cliente = new Cliente { Id = 1, Nome = "Cliente", Ativo = true };

        // OS 1: Bomba d'água custo R$ 50 (5000), cobrado R$ 120 (12000) -> líquido peça R$ 70 (7000)
        // + Mão de obra R$ 150 (15000) -> Total OS R$ 270 (27000), Líquido operacional R$ 220 (22000)
        var os1 = OrdemServico.Criar(cliente, "AAA0A00", "VW", "Gol", "Prata", 2018, "Bomba",
            [new PecaOrdemServico { Nome = "Bomba d'água", Quantidade = 1, ValorUnitarioCentavos = 0, ValorTotalCentavos = 12000, CustoUnitarioCentavos = 5000 }],
            15000, false, null);
        os1.Id = 1;
        os1.Finalizar(new DateTime(2026, 11, 15, 14, 0, 0, DateTimeKind.Utc));

        // OS 2: Somente mão de obra R$ 80 (8000), sem peças
        var os2 = OrdemServico.Criar(cliente, "BBB1B11", "Fiat", "Uno", "Branco", 2019, "Alinhamento",
            [], 8000, false, null);
        os2.Id = 2;
        os2.Finalizar(new DateTime(2026, 11, 16, 14, 0, 0, DateTimeKind.Utc));

        // OS 3: Histórica com peça de custo desconhecido (null) — nunca pode entrar como custo zero!
        var os3 = new OrdemServico
        {
            Id = 3,
            Status = StatusOrdemServico.Concluida,
            DataConclusaoUtc = new DateTime(2026, 11, 17, 14, 0, 0, DateTimeKind.Utc),
            MaoDeObraCentavos = 5000,
            Pecas = [new PecaOrdemServico { Nome = "Peça antiga", Quantidade = 2, ValorUnitarioCentavos = 3000, CustoUnitarioCentavos = null }]
        };
        // Simula TotalCentavos via reflexão/propriedade ou usando entidade com total conhecido:
        // TotalCentavos tem setter privado; vamos validar que BrutoPecasSemCusto = 6000 e LiquidoPecas não inclui esses 6000.

        var fake = new RelatorioFake([os1, os2, os3], orcamentosPendentes: 2);
        var service = new RelatorioFinanceiroService(fake);

        var inicio = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        var fim = new DateTime(2026, 11, 30, 23, 59, 59, DateTimeKind.Utc);
        var relatorio = await service.GerarAsync(inicio, fim, "conclusao", default);

        Assert.Equal(3, relatorio.QuantidadeOrdens);
        Assert.Equal(2, relatorio.QuantidadeOrdensComPecas);
        Assert.Equal(1, relatorio.QuantidadeOrdensSomenteMaoDeObra);
        Assert.Equal(2, relatorio.QuantidadeOrcamentosPendentes);

        Assert.Equal(18000, relatorio.BrutoPecasCentavos);      // 12000 (com custo) + 6000 (sem custo)
        Assert.Equal(5000, relatorio.CustoPecasCentavos);       // apenas os1
        Assert.Equal(7000, relatorio.LiquidoPecasCentavos);     // 12000 - 5000 (sem contaminar com os3)
        Assert.Equal(28000, relatorio.MaoDeObraCentavos);       // 15000 + 8000 + 5000
        Assert.Equal(35000, relatorio.LiquidoOperacionalCentavos); // 7000 + 28000

        Assert.Equal(1, relatorio.QuantidadeOrdensComCustoDesconhecido);
        Assert.Equal(1, relatorio.QuantidadePecasComCustoDesconhecido);
        Assert.Equal(6000, relatorio.BrutoPecasSemCustoCentavos);
    }

    [Fact]
    public async Task EndpointRelatorioValidaParametrosERetorna200()
    {
        var fake = new RelatorioFake([], orcamentosPendentes: 1);
        using var factory = new ApiFactory();
        using var client = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IConsultaOrdemRepository>();
            services.AddSingleton<IConsultaOrdemRepository>(fake);
        })).CreateClient();

        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.GetAsync("/api/v1/relatorios/financeiro?criterio=invalido")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.GetAsync("/api/v1/relatorios/financeiro?inicioUtc=2026-11-20T00:00:00Z&fimUtc=2026-11-10T00:00:00Z")).StatusCode);

        var resp = await client.GetFromJsonAsync<RelatorioFinanceiroResponse>(
            "/api/v1/relatorios/financeiro?inicioUtc=2026-11-01T00:00:00Z&fimUtc=2026-11-30T23:59:59Z&criterio=abertura");

        Assert.NotNull(resp);
        Assert.Equal("abertura", resp.Criterio);
        Assert.Equal(1, resp.QuantidadeOrcamentosPendentes);
        Assert.False(fake.UltimoPorConclusao);
    }

    private sealed class RelatorioFake(List<OrdemServico> ordens, int orcamentosPendentes) : IConsultaOrdemRepository
    {
        public bool UltimoPorConclusao { get; private set; }

        public Task<List<OrdemServico>> ListarAsync(int pagina, string? busca, StatusOrdemServico? status, CancellationToken ct) =>
            Task.FromResult(new List<OrdemServico>());
        public Task<OrdemServico?> ObterAsync(long id, CancellationToken ct) => Task.FromResult<OrdemServico?>(null);
        public Task<List<OrdemServico>> ListarRetornosAsync(DateTime limiteUtc, int limiteItens, CancellationToken ct) =>
            Task.FromResult(new List<OrdemServico>());
        public Task<List<OrdemServico>> ListarPorPlacaAsync(string placaNormalizada, int limiteItens, CancellationToken ct) =>
            Task.FromResult(new List<OrdemServico>());
        public Task<List<OrdemServico>> ListarParaRelatorioAsync(DateTime inicioUtc, DateTime fimUtc, bool porConclusao, CancellationToken ct)
        {
            UltimoPorConclusao = porConclusao;
            return Task.FromResult(ordens);
        }
        public Task<int> ContarOrcamentosPendentesAsync(DateTime inicioUtc, DateTime fimUtc, CancellationToken ct) =>
            Task.FromResult(orcamentosPendentes);
    }
}
