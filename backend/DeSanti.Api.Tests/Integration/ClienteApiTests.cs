using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DeSanti.Application.DTOs.Cliente;
using DeSanti.Application.Interfaces.Repositories;
using DeSanti.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DeSanti.Api.Tests.Integration;

public class ClienteApiTests
{
    [Theory]
    [InlineData("123.456.789-01", "12345678901", "(11) 98765-4321", "11987654321")]
    [InlineData("12.345.678/0001-99", "12345678000199", "(11) 3456-7890", "1134567890")]
    public async Task CriaClienteComCpfOuCnpjNormalizadoESemEmail(
        string documentoEnviado,
        string documentoEsperado,
        string telefoneEnviado,
        string telefoneEsperado)
    {
        using var factory = new ClienteApiFactory();
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/clientes", new CreateClienteDto
        {
            Nome = "  Cliente Oficina  ",
            Documento = documentoEnviado,
            Telefone = telefoneEnviado,
            Email = ""
        });
        var criado = await response.Content.ReadFromJsonAsync<ResponseClienteDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(criado);
        Assert.Equal(7, criado.Id);
        Assert.Equal("Cliente Oficina", criado.Nome);
        Assert.Equal(documentoEsperado, criado.Documento);
        Assert.Equal(telefoneEsperado, criado.Telefone);
        Assert.Equal("", criado.Email);
        Assert.True(criado.Ativo);
    }

    [Fact]
    public async Task RejeitaClienteInvalidoCom400ValidationProblemDetails()
    {
        using var factory = new ClienteApiFactory();
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/clientes", new CreateClienteDto
        {
            Nome = "",
            Documento = "123",
            Telefone = "999",
            Email = ""
        });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = body.GetProperty("errors");
        Assert.True(errors.TryGetProperty("nome", out _));
        Assert.True(errors.TryGetProperty("documento", out _));
        Assert.True(errors.TryGetProperty("telefone", out _));
        Assert.Null(factory.Clientes.Salvo);
    }

    [Fact]
    public async Task AtualizaClienteNormalizandoCamposERetorna204()
    {
        using var factory = new ClienteApiFactory();
        factory.Clientes.Banco[7] = new Cliente
        {
            Id = 7,
            Nome = "Cliente Antigo",
            Documento = "00000000000",
            Telefone = "0000000000",
            Email = "",
            Ativo = true
        };
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/v1/clientes", new UpdateClienteDto
        {
            Id = 7,
            Nome = "  Cliente Atualizado  ",
            Documento = "123.456.789-01",
            Telefone = "(11) 98765-4321",
            Email = "",
            Ativo = false
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var salvo = factory.Clientes.Banco[7];
        Assert.Equal("Cliente Atualizado", salvo.Nome);
        Assert.Equal("12345678901", salvo.Documento);
        Assert.Equal("11987654321", salvo.Telefone);
        Assert.False(salvo.Ativo);
        Assert.NotNull(salvo.DataAlteracao);
    }

    [Fact]
    public async Task AtualizacaoComDadosInvalidosRetorna400ValidationProblemDetails()
    {
        using var factory = new ClienteApiFactory();
        factory.Clientes.Banco[7] = new Cliente
        {
            Id = 7,
            Nome = "Cliente Original",
            Documento = "00000000000",
            Telefone = "11999999999",
            Ativo = true
        };
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/v1/clientes", new UpdateClienteDto
        {
            Id = 7,
            Nome = " ",
            Documento = "123",
            Telefone = "999",
            Email = "",
            Ativo = true
        });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = body.GetProperty("errors");
        Assert.True(errors.TryGetProperty("nome", out _));
        Assert.True(errors.TryGetProperty("documento", out _));
        Assert.True(errors.TryGetProperty("telefone", out _));
        Assert.Equal("Cliente Original", factory.Clientes.Banco[7].Nome);
    }

    [Fact]
    public async Task AtualizacaoDeClienteInexistenteRetorna404()
    {
        using var factory = new ClienteApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/v1/clientes", new UpdateClienteDto
        {
            Id = 999,
            Nome = "Cliente Inexistente",
            Documento = "12345678901",
            Telefone = "11987654321",
            Email = "",
            Ativo = true
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AtualizacaoComDocumentoDeOutroClienteRetorna409()
    {
        using var factory = new ClienteApiFactory();
        factory.Clientes.Banco[7] = new Cliente
        {
            Id = 7,
            Nome = "Cliente Um",
            Documento = "11111111111",
            Telefone = "11911111111",
            Ativo = true
        };
        factory.Clientes.Banco[8] = new Cliente
        {
            Id = 8,
            Nome = "Cliente Dois",
            Documento = "22222222222",
            Telefone = "11922222222",
            Ativo = true
        };
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/v1/clientes", new UpdateClienteDto
        {
            Id = 7,
            Nome = "Cliente Um",
            Documento = "222.222.222-22",
            Telefone = "11911111111",
            Email = "",
            Ativo = true
        });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.True(body.GetProperty("errors").TryGetProperty("documento", out _));
    }

    private sealed class ClienteApiFactory : WebApplicationFactory<Program>
    {
        public FakeClienteRepository Clientes { get; } = new();

        protected override void ConfigureClient(HttpClient client)
        {
            base.ConfigureClient(client);
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ApiFactory.GerarTokenTeste());
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        "Server=127.0.0.1;Database=desanti_test;User=test;Password=test;"
                }));

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IClienteRepository>();
                services.AddSingleton<IClienteRepository>(Clientes);
            });
        }
    }

    private sealed class FakeClienteRepository : IClienteRepository
    {
        public Dictionary<long, Cliente> Banco { get; } = [];
        public Cliente? Salvo { get; private set; }

        public Task AddAsync(Cliente cliente)
        {
            cliente.Id = 7;
            Salvo = cliente;
            Banco[cliente.Id] = cliente;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Cliente cliente)
        {
            Banco[cliente.Id] = cliente;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Cliente cliente) => Task.CompletedTask;
        public Task<List<Cliente>> GetAllAsync() => Task.FromResult(Banco.Values.ToList());
        public Task<Cliente?> GetByIdAsync(long id) => Task.FromResult(Banco.GetValueOrDefault(id));
        public Task<bool> ExisteComDocumentoAsync(string documentoNormalizado, long? ignorarId = null) =>
            Task.FromResult(Banco.Values.Any(c =>
                (!ignorarId.HasValue || c.Id != ignorarId.Value) &&
                DeSanti.Application.Validators.Clientes.CriarClienteValidator.SomenteDigitos(c.Documento) == documentoNormalizado));
    }
}
