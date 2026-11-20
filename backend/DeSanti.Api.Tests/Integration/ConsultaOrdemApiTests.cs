using System.Net;
using System.Net.Http.Json;
using DeSanti.Application.DTOs.OrdensServico;
using DeSanti.Application.Interfaces.Repositories;
using DeSanti.Domain;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DeSanti.Api.Tests.Integration;

public class ConsultaOrdemApiTests
{
    [Fact]
    public async Task DetalheRetornaSnapshotEPecas()
    {
        using var factory = new ApiFactory();
        using var client = CriarClient(factory);
        var ordem = await client.GetFromJsonAsync<ConsultaOrdemResponse>("/api/v1/ordens-servico/7");

        Assert.NotNull(ordem);
        Assert.Equal("Cliente fictício", ordem.NomeCliente);
        Assert.Equal(10000L, Assert.Single(ordem.Pecas).ValorTotalCentavos);
    }

    [Fact]
    public async Task ConsultaRejeitaPaginaInvalidaERetorna404ParaOsAusente()
    {
        using var factory = new ApiFactory();
        using var client = CriarClient(factory);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/ordens-servico?pagina=0")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/ordens-servico?status=invalido")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/v1/ordens-servico/8")).StatusCode);
    }

    [Theory]
    [InlineData("orcamento", StatusOrdemServico.Orcamento)]
    [InlineData("aberta", StatusOrdemServico.Aberta)]
    [InlineData("concluida", StatusOrdemServico.Concluida)]
    public async Task ListagemRepassaBuscaStatusEPaginaESerializaDatas(string status, StatusOrdemServico esperadoStatus)
    {
        var fake = new ConsultaFake();
        using var factory = new ApiFactory();
        using var client = CriarClient(factory, fake);

        var ordens = await client.GetFromJsonAsync<List<ConsultaOrdemResponse>>(
            $"/api/v1/ordens-servico?pagina=2&busca=AAA0A00&status={status}");

        Assert.NotNull(ordens);
        Assert.Equal((2, "AAA0A00", esperadoStatus), fake.UltimaConsulta);
        var item = Assert.Single(ordens);
        Assert.Equal(7, item.Id);
        Assert.NotNull(item.DataConclusaoUtc);
        Assert.NotNull(item.DataPrevistaRetornoUtc);
    }

    [Fact]
    public async Task ListagemExpoeHeaderDeProximaPagina()
    {
        var fake = new ConsultaFake { TotalItensLista = 21 };
        using var factory = new ApiFactory();
        using var client = CriarClient(factory, fake);

        var response = await client.GetAsync("/api/v1/ordens-servico?pagina=1");
        var ordens = await response.Content.ReadFromJsonAsync<List<ConsultaOrdemResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("true", Assert.Single(response.Headers.GetValues("X-Has-Next-Page")));
        Assert.Equal(20, ordens!.Count);
    }

    [Fact]
    public async Task RetornosRetornaListaEValidaParametroDias()
    {
        using var factory = new ApiFactory();
        using var client = CriarClient(factory);

        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/ordens-servico/retornos?dias=0")).StatusCode);

        var retornos = await client.GetFromJsonAsync<List<RetornoOrdemResponse>>("/api/v1/ordens-servico/retornos?dias=14");
        Assert.NotNull(retornos);
        var item = Assert.Single(retornos);
        Assert.Equal(7, item.Id);
        Assert.Equal("AAA0A00", item.Placa);
    }

    [Fact]
    public async Task HistoricoVeiculoValidaPlacaERetorna404QuandoSemRegistros()
    {
        using var factory = new ApiFactory();
        using var client = CriarClient(factory);

        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/ordens-servico/veiculo/123")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/v1/ordens-servico/veiculo/BBB1B11")).StatusCode);

        var historico = await client.GetFromJsonAsync<HistoricoVeiculoResponse>("/api/v1/ordens-servico/veiculo/AAA-0A00");
        Assert.NotNull(historico);
        Assert.Equal("AAA0A00", historico.Placa);
        Assert.Equal("Fiat", historico.Marca);
        Assert.Single(historico.UltimasOrdens);
    }

    private static HttpClient CriarClient(ApiFactory factory, ConsultaFake? fake = null) =>
        factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IConsultaOrdemRepository>();
            services.AddSingleton<IConsultaOrdemRepository>(fake ?? new ConsultaFake());
        })).CreateClient();

    private sealed class ConsultaFake : IConsultaOrdemRepository
    {
        public (int Pagina, string? Busca, StatusOrdemServico? Status)? UltimaConsulta { get; private set; }
        public int TotalItensLista { get; set; } = 1;

        public Task<List<OrdemServico>> ListarAsync(int pagina, string? busca, StatusOrdemServico? status, CancellationToken ct)
        {
            UltimaConsulta = (pagina, busca, status);
            var conclusao = new DateTime(2026, 11, 13, 12, 0, 0, DateTimeKind.Utc);
            return Task.FromResult(Enumerable.Range(0, TotalItensLista).Select(i => new OrdemServico
            {
                Id = 7 + i,
                NomeCliente = "Cliente fictício",
                Placa = "AAA0A00",
                DataCriacaoUtc = conclusao.AddHours(-2),
                DataConclusaoUtc = conclusao,
                DataPrevistaRetornoUtc = conclusao.AddDays(30),
                NecessitaRetorno = true,
                PrazoRetornoDias = 30,
                Status = StatusOrdemServico.Concluida
            }).ToList());
        }

        public Task<OrdemServico?> ObterAsync(long id, CancellationToken ct) =>
            Task.FromResult<OrdemServico?>(id == 7 ? new OrdemServico
            {
                Id = 7,
                NomeCliente = "Cliente fictício",
                Pecas = [new PecaOrdemServico { Nome = "Peça fictícia", Quantidade = 3, ValorTotalCentavos = 10000 }]
            } : null);

        public Task<List<OrdemServico>> ListarRetornosAsync(DateTime limiteUtc, int limiteItens, CancellationToken ct)
        {
            var conclusao = new DateTime(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);
            return Task.FromResult<List<OrdemServico>>([
                new OrdemServico
                {
                    Id = 7,
                    NomeCliente = "Cliente fictício",
                    TelefoneCliente = "11999999999",
                    Placa = "AAA0A00",
                    Marca = "Fiat",
                    Modelo = "Uno",
                    DescricaoServico = "Troca de óleo",
                    DataConclusaoUtc = conclusao,
                    DataPrevistaRetornoUtc = conclusao.AddDays(5),
                    NecessitaRetorno = true,
                    PrazoRetornoDias = 5
                }
            ]);
        }

        public Task<List<OrdemServico>> ListarPorPlacaAsync(string placaNormalizada, int limiteItens, CancellationToken ct) =>
            Task.FromResult<List<OrdemServico>>(placaNormalizada == "AAA0A00"
                ? [new OrdemServico
                {
                    Id = 7,
                    ClienteId = 1,
                    NomeCliente = "Cliente fictício",
                    Placa = "AAA0A00",
                    Marca = "Fiat",
                    Modelo = "Uno",
                    Cor = "Prata",
                    Ano = 2020,
                    DataCriacaoUtc = new DateTime(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc)
                }]
                : []);

        public Task<List<OrdemServico>> ListarParaRelatorioAsync(DateTime inicioUtc, DateTime fimUtc, bool porConclusao, CancellationToken ct) =>
            Task.FromResult(new List<OrdemServico>());

        public Task<int> ContarOrcamentosPendentesAsync(DateTime inicioUtc, DateTime fimUtc, CancellationToken ct) =>
            Task.FromResult(0);
    }
}
