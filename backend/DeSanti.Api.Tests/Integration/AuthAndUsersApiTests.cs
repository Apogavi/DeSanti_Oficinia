using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DeSanti.Application.DTOs.Auth;
using DeSanti.Application.DTOs.Usuarios;
using DeSanti.Application.Interfaces.Repositories;
using DeSanti.Application.Security;
using DeSanti.Domain;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DeSanti.Api.Tests.Integration;

public class AuthAndUsersApiTests
{
    [Fact]
    public async Task EndpointProtegidoSemTokenRetorna401()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;

        var respClientes = await client.GetAsync("/api/v1/clientes");
        var respOrdens = await client.GetAsync("/api/v1/ordens-servico");
        var respMe = await client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, respClientes.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, respOrdens.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, respMe.StatusCode);
    }

    [Fact]
    public async Task LoginMeAlterarSenhaEPerfilFuncionamDePontaAPonta()
    {
        var fakeUsers = new UsuarioRepositoryFake();
        using var factory = new ApiFactory();
        using var client = CriarClient(factory, fakeUsers);
        client.DefaultRequestHeaders.Authorization = null;

        // Senha incorreta retorna 401
        var falha = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("desanti", "errada"));
        Assert.Equal(HttpStatusCode.Unauthorized, falha.StatusCode);

        // Login válido retorna JWT e perfil
        var ok = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("DeSanti", "SenhaForte123!"));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var loginResp = await ok.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginResp);
        Assert.False(string.IsNullOrWhiteSpace(loginResp.Token));
        Assert.Equal("desanti", loginResp.User.UserName);
        Assert.Equal(AppRoles.Admin, loginResp.User.Role);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResp.Token);

        var me = await client.GetFromJsonAsync<UsuarioPerfilDto>("/api/v1/auth/me");
        Assert.NotNull(me);
        Assert.Equal("desanti", me.UserName);

        // Alterar própria senha
        var troca = await client.PostAsJsonAsync("/api/v1/auth/change-password",
            new AlterarSenhaRequest("SenhaForte123!", "NovaSenhaForte456!"));
        Assert.Equal(HttpStatusCode.NoContent, troca.StatusCode);

        // Login com a nova senha funciona
        var novoLogin = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("desanti", "NovaSenhaForte456!"));
        Assert.Equal(HttpStatusCode.OK, novoLogin.StatusCode);
    }

    [Fact]
    public async Task OperadorRecebe403EmRelatoriosFinanceirosEGestaoDeUsuarios()
    {
        var fakeUsers = new UsuarioRepositoryFake();
        using var factory = new ApiFactory();
        using var client = CriarClient(factory, fakeUsers);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ApiFactory.GerarTokenTeste(AppRoles.Operador, 2, "operador"));

        var respRel = await client.GetAsync("/api/v1/relatorios/financeiro");
        var respUsers = await client.GetAsync("/api/v1/usuarios");

        Assert.Equal(HttpStatusCode.Forbidden, respRel.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, respUsers.StatusCode);
    }

    [Fact]
    public async Task ImpedeDesativarOuRebaixarUltimoAdminAtivo()
    {
        var fakeUsers = new UsuarioRepositoryFake();
        using var factory = new ApiFactory();
        using var client = CriarClient(factory, fakeUsers);

        // Existe apenas 1 Admin ativo no fakeUsers (id = 1). Tentar desativá-lo deve retornar 400.
        var desativarUltimo = await client.PutAsJsonAsync("/api/v1/usuarios/1",
            new AtualizarUsuarioRequest("desanti", "Dono DeSanti", null, AppRoles.Admin, false));
        Assert.Equal(HttpStatusCode.BadRequest, desativarUltimo.StatusCode);

        // Criar segundo Admin ("diosec") permite editar normalmente
        var criarDiosec = await client.PostAsJsonAsync("/api/v1/usuarios",
            new CriarUsuarioRequest("diosec", "Diogo", null, "SenhaSuperForte123!", AppRoles.Admin));
        Assert.Equal(HttpStatusCode.Created, criarDiosec.StatusCode);
    }

    private static HttpClient CriarClient(ApiFactory factory, UsuarioRepositoryFake fake) =>
        factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IUsuarioRepository>();
            services.AddSingleton<IUsuarioRepository>(fake);
        })).CreateClient();

    private sealed class UsuarioRepositoryFake : IUsuarioRepository
    {
        public Dictionary<long, Usuario> Banco { get; } = new()
        {
            [1] = new Usuario
            {
                Id = 1,
                NomeUsuario = "desanti",
                Nome = "DeSanti Automotiva",
                PasswordHash = SenhaHasher.Hash("SenhaForte123!"),
                Role = AppRoles.Admin,
                Ativo = true,
                DataCriacaoUtc = DateTime.UtcNow
            }
        };

        public Task<Usuario?> GetByIdAsync(long id, CancellationToken ct = default) =>
            Task.FromResult(Banco.GetValueOrDefault(id));

        public Task<Usuario?> GetByUserNameAsync(string nomeUsuarioNormalizado, CancellationToken ct = default) =>
            Task.FromResult(Banco.Values.FirstOrDefault(u => u.NomeUsuario == nomeUsuarioNormalizado));

        public Task<List<Usuario>> GetAllAsync(CancellationToken ct = default) =>
            Task.FromResult(Banco.Values.ToList());

        public Task<bool> ExisteComUserNameAsync(string nomeUsuarioNormalizado, long? ignorarId = null, CancellationToken ct = default) =>
            Task.FromResult(Banco.Values.Any(u =>
                (!ignorarId.HasValue || u.Id != ignorarId.Value) && u.NomeUsuario == nomeUsuarioNormalizado));

        public Task<int> ContarAdminsAtivosAsync(long? ignorarId = null, CancellationToken ct = default) =>
            Task.FromResult(Banco.Values.Count(u =>
                u.Ativo && u.Role == AppRoles.Admin && (!ignorarId.HasValue || u.Id != ignorarId.Value)));

        public Task<bool> AnyAsync(CancellationToken ct = default) =>
            Task.FromResult(Banco.Count > 0);

        public Task AddAsync(Usuario usuario, CancellationToken ct = default)
        {
            usuario.Id = Banco.Keys.DefaultIfEmpty(0).Max() + 1;
            Banco[usuario.Id] = usuario;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Usuario usuario, CancellationToken ct = default)
        {
            Banco[usuario.Id] = usuario;
            return Task.CompletedTask;
        }
    }
}
