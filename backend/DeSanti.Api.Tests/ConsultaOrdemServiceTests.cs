using DeSanti.Application;
using DeSanti.Application.Interfaces.Repositories;
using DeSanti.Domain;
using Xunit;

namespace DeSanti.Api.Tests;
public class ConsultaOrdemServiceTests
{
    [Fact]
    public async Task DetalhePreservaHistoricoECustoDesconhecido()
    {
        var service = new ConsultaOrdemService(new Fake());
        var detalhe = await service.ObterAsync(1, default);
        Assert.NotNull(detalhe);
        Assert.Equal("Nome histórico", detalhe.NomeCliente);
        Assert.Null(detalhe.Pecas.Single().CustoUnitarioCentavos);
    }

    [Fact]
    public async Task OrdemInexistenteRetornaNull()
    {
        Assert.Null(await new ConsultaOrdemService(new Fake()).ObterAsync(2, default));
    }

    [Fact]
    public async Task ListarPaginadoDetectaProximaPaginaSemDevolverVigesimoPrimeiroItem()
    {
        var fake = new Fake { QuantidadeLista = 21 };
        var service = new ConsultaOrdemService(fake);

        var pagina = await service.ListarPaginadoAsync(1, null, null, default);

        Assert.Equal(20, pagina.Itens.Count);
        Assert.True(pagina.TemProxima);
    }

    [Fact]
    public async Task ListarPaginadoComVinteItensExatosMarcaSemProxima()
    {
        var fake = new Fake { QuantidadeLista = 20 };
        var service = new ConsultaOrdemService(fake);

        var pagina = await service.ListarPaginadoAsync(1, null, null, default);

        Assert.Equal(20, pagina.Itens.Count);
        Assert.False(pagina.TemProxima);
    }

    [Fact]
    public async Task HistoricoVeiculoNormalizaPlacaERetornaUltimoRegistro()
    {
        var service = new ConsultaOrdemService(new Fake());
        var historico = await service.ObterHistoricoVeiculoAsync("aaa-0a00", default);

        Assert.NotNull(historico);
        Assert.Equal("AAA0A00", historico.Placa);
        Assert.Equal("Fiat", historico.Marca);
        Assert.Equal("Uno", historico.Modelo);
        Assert.Single(historico.UltimasOrdens);
    }

    private sealed class Fake : IConsultaOrdemRepository
    {
        public int QuantidadeLista { get; set; }

        public Task<List<OrdemServico>> ListarAsync(int pagina, string? busca, StatusOrdemServico? status, CancellationToken ct) =>
            Task.FromResult(Enumerable.Range(1, QuantidadeLista).Select(i => new OrdemServico { Id = i }).ToList());

        public Task<OrdemServico?> ObterAsync(long id, CancellationToken ct) =>
            Task.FromResult<OrdemServico?>(id == 1 ? new OrdemServico {
                NomeCliente = "Nome histórico",
                Cliente = new Cliente { Nome = "Nome atual" },
                Pecas = [new PecaOrdemServico { Nome = "Peça antiga", Quantidade = 1,
                    ValorUnitarioCentavos = 100, CustoUnitarioCentavos = null }]
            } : null);

        public Task<List<OrdemServico>> ListarRetornosAsync(DateTime limiteUtc, int limiteItens, CancellationToken ct) =>
            Task.FromResult(new List<OrdemServico>());

        public Task<List<OrdemServico>> ListarPorPlacaAsync(string placaNormalizada, int limiteItens, CancellationToken ct) =>
            Task.FromResult<List<OrdemServico>>(placaNormalizada == "AAA0A00"
                ? [new OrdemServico { Id = 10, ClienteId = 3, NomeCliente = "Cliente teste", Placa = "AAA0A00", Marca = "Fiat", Modelo = "Uno", Cor = "Prata", Ano = 2015 }]
                : []);

        public Task<List<OrdemServico>> ListarParaRelatorioAsync(DateTime inicioUtc, DateTime fimUtc, bool porConclusao, CancellationToken ct) =>
            Task.FromResult(new List<OrdemServico>());

        public Task<int> ContarOrcamentosPendentesAsync(DateTime inicioUtc, DateTime fimUtc, CancellationToken ct) =>
            Task.FromResult(0);
    }
}
