using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DeSanti.Application.DTOs.OrdensServico;
using DeSanti.Application.Interfaces.Repositories;
using DeSanti.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DeSanti.Api.Tests.Integration;

/// <summary>Testa routing, JSON, DI e use case com Repositories simulados, sem banco real.</summary>
public class OrdemServicoApiTests
{
    [Theory]
    [InlineData("00000000000")]
    [InlineData("000.000.000-00")]
    public async Task DocumentoDuplicadoRetorna409(string documento)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/clientes",
            new { nome = "Outro cliente", documento, telefone = "00000000000", email = "" });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("errors").TryGetProperty("documento", out _));
        Assert.Null(factory.Clientes.Adicionado);
    }

    [Fact]
    public async Task CriaOsComTotalOficialEDadosDoCadastro()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/ordens-servico", PedidoValido());
        var result = await response.Content.ReadFromJsonAsync<CriarOrdemServicoResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(42, result!.Id);
        Assert.Equal(12303, result.TotalCentavos);
        Assert.Equal("Cliente Teste", factory.Ordens.Salva!.NomeCliente);
        Assert.Equal(1, factory.Ordens.Salva.ClienteId);
        Assert.Single(factory.Ordens.Salva.Pecas);
        Assert.Null(factory.Ordens.Salva.DataPrevistaRetornoUtc);
        Assert.Equal(1, factory.Ordens.Chamadas);
    }

    [Theory]
    [InlineData(0, "clienteId")]
    [InlineData(1, "totalCentavos")]
    public async Task Retorna400ComErrosDeCampoSemPersistir(long clienteId, string campo)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var pedido = PedidoValido() with { ClienteId = clienteId };
        if (campo == "totalCentavos")
            pedido = pedido with { Pecas = [new PecaRequest("Peça teste", 2, long.MaxValue, 0)] };
        var response = await client.PostAsJsonAsync("/api/v1/ordens-servico", pedido);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(body.GetProperty("errors").TryGetProperty(campo, out _));
        Assert.Equal(0, factory.Ordens.Chamadas);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(70L)]
    public async Task RecebeCustoObrigatorioEPreservaNoRepository(long custo)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var pedido = PedidoValido() with { Pecas = [new PecaRequest("Peça teste", 3, 101, custo)] };
        var response = await client.PostAsJsonAsync("/api/v1/ordens-servico", pedido);
        var result = await response.Content.ReadFromJsonAsync<CriarOrdemServicoResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(12303, result!.TotalCentavos);
        Assert.Equal(custo, Assert.Single(factory.Ordens.Salva!.Pecas).CustoUnitarioCentavos);
    }

    [Fact]
    public async Task CustoNuloRetorna400SemPersistir()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var pedido = PedidoValido() with { Pecas = [new PecaRequest("Peça teste", 3, 101, null)] };
        var response = await client.PostAsJsonAsync("/api/v1/ordens-servico", pedido);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(body.GetProperty("errors").TryGetProperty("pecas[0].custoUnitarioCentavos", out _));
        Assert.Equal(0, factory.Ordens.Chamadas);
    }

    [Fact]
    public async Task RequestSemCampoCustoRetorna400SemPersistir()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var json = JsonSerializer.SerializeToNode(PedidoValido(), new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        json["pecas"]![0]!.AsObject().Remove("custoUnitarioCentavos");
        using var content = new StringContent(json.ToJsonString(), System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/v1/ordens-servico", content);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Campo omitido não pode virar custo zero: o contrato exige custo conhecido.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(body.GetProperty("errors").TryGetProperty("pecas[0].custoUnitarioCentavos", out _));
        Assert.Equal(0, factory.Ordens.Chamadas);
    }

    [Fact]
    public async Task CustoNegativoRetorna400SemPersistir()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var pedido = PedidoValido() with { Pecas = [new PecaRequest("Peça teste", 3, 101, -1)] };
        var response = await client.PostAsJsonAsync("/api/v1/ordens-servico", pedido);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(body.GetProperty("errors").TryGetProperty("pecas[0].custoUnitarioCentavos", out _));
        Assert.Equal(0, factory.Ordens.Chamadas);
    }

    [Fact]
    public async Task ClienteInexistenteRetorna404()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/ordens-servico", PedidoValido() with { ClienteId = 999 });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, factory.Ordens.Chamadas);
    }

    [Fact]
    public async Task ClienteInativoRetorna409()
    {
        using var factory = new ApiFactory();
        factory.Clientes.Cliente.Ativo = false;
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/ordens-servico", PedidoValido());
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(0, factory.Ordens.Chamadas);
    }

    [Fact]
    public async Task ListaClientesUsandoApplicationRegistradaNoStartup()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/clientes");
        var clientes = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, clientes[0].GetProperty("id").GetInt64());
        Assert.True(clientes[0].GetProperty("ativo").GetBoolean());
    }

    [Fact]
    public async Task CriaClienteAtivoConformeDefaultDoMapping()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/clientes", new
        {
            nome = "Cliente Teste",
            documento = "00000000001",
            telefone = "00000000000",
            email = ""
        });
        var cliente = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(cliente.GetProperty("ativo").GetBoolean());
        Assert.True(factory.Clientes.Adicionado!.Ativo);
    }

    [Fact]
    public async Task FalhaInesperadaRetornaProblemDetailsSemExporDetalhes()
    {
        using var factory = new ApiFactory();
        factory.Ordens.Falhar = true;
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/ordens-servico", PedidoValido());
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        Assert.DoesNotContain("Detalhe interno de teste", content);
    }

    [Fact]
    public async Task FinalizarOsAbertaRetorna200ECalculaRetorno()
    {
        using var factory = new ApiFactory();
        factory.Ordens.PorId[42] = OrdemServico.Criar(
            factory.Clientes.Cliente, "AAA0A00", "Marca Teste", "Modelo Teste", "Azul",
            null, "Troca de correia", [], 15000, true, 30);
        factory.Ordens.PorId[42].Id = 42;
        using var client = factory.CreateClient();

        var antes = DateTime.UtcNow;
        var response = await client.PostAsync("/api/v1/ordens-servico/42/finalizar", null);
        var depois = DateTime.UtcNow;
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(42, body.GetProperty("id").GetInt64());
        var conclusao = body.GetProperty("dataConclusaoUtc").GetDateTime().ToUniversalTime();
        var retorno = body.GetProperty("dataPrevistaRetornoUtc").GetDateTime().ToUniversalTime();
        Assert.InRange(conclusao, antes.AddSeconds(-1), depois.AddSeconds(1));
        Assert.Equal(conclusao.AddDays(30), retorno);
    }

    [Fact]
    public async Task FinalizarOsSemRetornoRetorna200ComPrevisaoNula()
    {
        using var factory = new ApiFactory();
        factory.Ordens.PorId[43] = OrdemServico.Criar(
            factory.Clientes.Cliente, "AAA0A00", "Marca Teste", "Modelo Teste", "Azul",
            null, "Balanceamento", [], 7000, false, null);
        factory.Ordens.PorId[43].Id = 43;
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/v1/ordens-servico/43/finalizar", null);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(43, body.GetProperty("id").GetInt64());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("dataPrevistaRetornoUtc").ValueKind);
    }

    [Fact]
    public async Task FinalizarOsInexistenteRetorna404()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/v1/ordens-servico/999/finalizar", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task FinalizarOsJaConcluidaRetorna409()
    {
        using var factory = new ApiFactory();
        var os = OrdemServico.Criar(
            factory.Clientes.Cliente, "AAA0A00", "Marca Teste", "Modelo Teste", "Azul",
            null, "Revisão", [], 10000, false, null);
        os.Id = 44;
        os.Finalizar(new DateTime(2026, 11, 10, 15, 0, 0, DateTimeKind.Utc));
        factory.Ordens.PorId[44] = os;
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/v1/ordens-servico/44/finalizar", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task FinalizarOsComConflitoConcorrenteRetorna409()
    {
        using var factory = new ApiFactory();
        var os = OrdemServico.Criar(
            factory.Clientes.Cliente, "AAA0A00", "Marca Teste", "Modelo Teste", "Azul",
            null, "Revisão", [], 10000, false, null);
        os.Id = 45;
        factory.Ordens.PorId[45] = os;
        factory.Ordens.SimularConflitoFinalizacao = true;
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/v1/ordens-servico/45/finalizar", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task OsSemPecasComMaoDeObraRetorna201ECalculaTotal()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var pedido = PedidoValido() with
        {
            DescricaoServico = "Alinhamento e balanceamento sem peças",
            Pecas = [],
            MaoDeObraCentavos = 15000
        };

        var response = await client.PostAsJsonAsync("/api/v1/ordens-servico", pedido);
        var result = await response.Content.ReadFromJsonAsync<CriarOrdemServicoResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(15000, result.TotalCentavos);
        Assert.Empty(factory.Ordens.Salva!.Pecas);
    }

    [Fact]
    public async Task IdempotencyKeyRepetidaRetornaMesmaOsSemDuplicar()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var req1 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/ordens-servico")
        {
            Content = JsonContent.Create(PedidoValido())
        };
        req1.Headers.Add("Idempotency-Key", "chave-unica-001");
        var resp1 = await client.SendAsync(req1);
        var body1 = await resp1.Content.ReadFromJsonAsync<CriarOrdemServicoResponse>();

        using var req2 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/ordens-servico")
        {
            Content = JsonContent.Create(PedidoValido())
        };
        req2.Headers.Add("Idempotency-Key", "chave-unica-001");
        var resp2 = await client.SendAsync(req2);
        var body2 = await resp2.Content.ReadFromJsonAsync<CriarOrdemServicoResponse>();

        Assert.Equal(HttpStatusCode.Created, resp1.StatusCode);
        Assert.Equal(HttpStatusCode.Created, resp2.StatusCode);
        Assert.Equal(body1!.Id, body2!.Id);
        Assert.Equal(body1.TotalCentavos, body2.TotalCentavos);
        Assert.Equal(1, factory.Ordens.Chamadas);
    }

    [Fact]
    public async Task OrcamentoPodeSerAprovadoMasNaoFinalizadoAntesDeAprovar()
    {
        using var factory = new ApiFactory();
        var orcamento = OrdemServico.Criar(
            factory.Clientes.Cliente, "AAA0A00", "Marca", "Modelo", "Azul",
            2022, "Orçamento freios", [], 10000, false, null, ehOrcamento: true);
        orcamento.Id = 50;
        factory.Ordens.PorId[50] = orcamento;
        using var client = factory.CreateClient();

        var finalizarAntes = await client.PostAsync("/api/v1/ordens-servico/50/finalizar", null);
        Assert.Equal(HttpStatusCode.Conflict, finalizarAntes.StatusCode);

        var aprovar = await client.PostAsync("/api/v1/ordens-servico/50/aprovar", null);
        var aprovadoBody = await aprovar.Content.ReadFromJsonAsync<AprovarOrdemServicoResponse>();
        Assert.Equal(HttpStatusCode.OK, aprovar.StatusCode);
        Assert.Equal(50, aprovadoBody!.Id);
        Assert.Equal("aberta", aprovadoBody.Status);

        var aprovarNovamente = await client.PostAsync("/api/v1/ordens-servico/50/aprovar", null);
        Assert.Equal(HttpStatusCode.Conflict, aprovarNovamente.StatusCode);

        var aprovarInexistente = await client.PostAsync("/api/v1/ordens-servico/999/aprovar", null);
        Assert.Equal(HttpStatusCode.NotFound, aprovarInexistente.StatusCode);
    }

    private static CriarOrdemServicoRequest PedidoValido() => new(
        1, "AAA0A00", "Marca Teste", "Modelo Teste", "Azul", null,
        "Serviço de teste", [new PecaRequest("Peça teste", 3, 101, 70)], 12000, false, null);
}

internal sealed class ApiFactory : WebApplicationFactory<Program>
{
    public ClienteRepositoryFake Clientes { get; } = new();
    public OrdemServicoRepositoryFake Ordens { get; } = new();

    public static string GerarTokenTeste(string role = AppRoles.Admin, long id = 1, string userName = "admin")
    {
        var generator = new DeSanti.Infrastructure.Identity.JwtTokenGenerator(
            Microsoft.Extensions.Options.Options.Create(new DeSanti.Infrastructure.Identity.JwtOptions
            {
                SecretKey = "DeSanti-Dev-Local-Jwt-Secret-Key-Change-In-Production-2026!"
            }));
        return generator.Generate(new Usuario
        {
            Id = id,
            NomeUsuario = userName,
            Nome = "Usuário Teste",
            Role = role,
            Ativo = true
        }).Token;
    }

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GerarTokenTeste());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        // Sobrescreve qualquer configuração local; os fakes não resolvem AppDbContext.
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = "" }));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IClienteRepository>();
            services.RemoveAll<IOrdemServicoRepository>();
            services.AddSingleton<IClienteRepository>(Clientes);
            services.AddSingleton<IOrdemServicoRepository>(Ordens);
        });
    }
}

internal sealed class OrdemServicoRepositoryFake : IOrdemServicoRepository
{
    public OrdemServico? Salva { get; private set; }
    public Dictionary<long, OrdemServico> PorId { get; } = [];
    public int Chamadas { get; private set; }
    public bool Falhar { get; set; }
    public bool SimularConflitoFinalizacao { get; set; }

    public Task AddAsync(OrdemServico ordemServico, CancellationToken cancellationToken = default)
    {
        Chamadas++;
        if (Falhar) throw new InvalidOperationException("Detalhe interno de teste");
        ordemServico.Id = 42;
        Salva = ordemServico;
        PorId[ordemServico.Id] = ordemServico;
        return Task.CompletedTask;
    }

    public Task<OrdemServico?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        Task.FromResult(PorId.GetValueOrDefault(id));

    public Task<OrdemServico?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(PorId.Values.FirstOrDefault(os => os.IdempotencyKey == idempotencyKey));

    public Task<bool> SalvarAprovacaoAsync(long id, CancellationToken cancellationToken = default)
    {
        if (!PorId.TryGetValue(id, out var os) || os.Status != StatusOrdemServico.Orcamento)
            return Task.FromResult(false);
        os.Status = StatusOrdemServico.Aberta;
        return Task.FromResult(true);
    }

    public Task<bool> SalvarFinalizacaoAsync(OrdemServico ordemServico, CancellationToken cancellationToken = default) =>
        Task.FromResult(!SimularConflitoFinalizacao);
}

internal sealed class ClienteRepositoryFake : IClienteRepository
{
    public Cliente? Adicionado { get; private set; }
    public Cliente Cliente { get; } = new()
    {
        Id = 1,
        Nome = "Cliente Teste",
        Documento = "00000000000",
        Telefone = "00000000000",
        Ativo = true
    };
    public Task<Cliente?> GetByIdAsync(long id) => Task.FromResult(id == Cliente.Id ? Cliente : null);
    public Task<List<Cliente>> GetAllAsync() => Task.FromResult<List<Cliente>>([Cliente]);
    public Task<bool> ExisteComDocumentoAsync(string documentoNormalizado, long? ignorarId = null) =>
        Task.FromResult((!ignorarId.HasValue || Cliente.Id != ignorarId.Value) &&
            DeSanti.Application.Validators.Clientes.CriarClienteValidator.SomenteDigitos(Cliente.Documento) == documentoNormalizado);
    public Task AddAsync(Cliente cliente)
    {
        cliente.Id = 2;
        Adicionado = cliente;
        return Task.CompletedTask;
    }
    public Task UpdateAsync(Cliente cliente) => throw new NotSupportedException();
    public Task DeleteAsync(Cliente cliente) => throw new NotSupportedException();
}
