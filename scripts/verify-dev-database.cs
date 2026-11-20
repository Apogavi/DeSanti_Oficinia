#:project ../backend/DeSanti.Infrastructure/DeSanti.Infrastructure.csproj
#:property Nullable=enable
#:property PublishAot=false
#:property PublishTrimmed=false

// Integration check EXPLÍCITO para o banco local desanti_dev.
// Cria registros sintéticos, verifica persistência/histórico/rollback e os remove no finally.
// Não altera schema, não executa migrations e recusa outros bancos/usuários/endereços.
// Requer API em localhost:5085 e configuração carregada por verify-dev-database.sh.
// Usa JIT: o check de development não configura EF Core/JSON para NativeAOT.
using System.Net;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Net.Http.Json;
using DeSanti.Application.DTOs.Cliente;
using DeSanti.Application.DTOs.OrdensServico;
using DeSanti.Domain;
using DeSanti.Infrastructure.Data;
using DeSanti.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

try
{
    var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
        ?? throw new CheckException("Configuração local não carregada.");
    var builder = new MySqlConnectionStringBuilder(connectionString) { ConnectionTimeout = 3 };
    Check(builder.Server == "127.0.0.1" && builder.Port == 3306 &&
        builder.Database == "desanti_dev" && builder.UserID == "desanti_app",
        "O teste aceita somente o banco local desanti_dev e usuário desanti_app.");
    await using (var connection = new MySqlConnection(builder.ConnectionString))
    {
        await connection.OpenAsync();
        using var version = new MySqlCommand("SELECT VERSION()", connection);
        var serverVersion = (await version.ExecuteScalarAsync())?.ToString();
        Console.WriteLine($"Servidor local: MySQL {serverVersion}; database desanti_dev.");
        Check(serverVersion?.StartsWith("8.4.") == true, "Versão MySQL inesperada.");
        using var schema = new MySqlCommand(
            "SELECT TABLE_NAME, ENGINE FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE()", connection);
        using var reader = await schema.ExecuteReaderAsync();
        var tables = new Dictionary<string, string>();
        while (await reader.ReadAsync()) tables.Add(reader.GetString(0), reader.GetString(1));
        foreach (var table in new[] { "Clientes", "OrdensServico", "PecasOrdemServico" })
            Check(tables.TryGetValue(table, out var engine) && engine == "InnoDB", $"Tabela InnoDB ausente: {table}.");
        Console.WriteLine("Schema: Clientes, OrdensServico e PecasOrdemServico presentes, todas InnoDB.");
    }

    var options = new DbContextOptionsBuilder<AppDbContext>().UseMySQL(builder.ConnectionString).Options;
    using var http = new HttpClient { BaseAddress = new Uri("http://localhost:5085"), Timeout = TimeSpan.FromSeconds(15) };
    long clienteId = 0;
    try
    {
        using var createCliente = await http.PostAsJsonAsync("api/v1/clientes", new CreateClienteDto
        {
            Nome = "Cliente Teste Integração",
            Documento = "00000000000",
            Telefone = "00000000000",
            Email = "integracao@example.invalid"
        });
        Check(createCliente.IsSuccessStatusCode, "Falha no cadastro do cliente fictício pela API.");
        var cliente = await createCliente.Content.ReadFromJsonAsync<ResponseClienteDto>();
        Check(cliente is not null && cliente.Id > 0, "API não retornou ID de cliente.");
        clienteId = cliente!.Id;
        Check(cliente.Ativo, "Cliente criado não está ativo.");

        var pedido = new CriarOrdemServicoRequest(clienteId, "AAA0A00", "Marca Teste", "Modelo Teste", "Azul", null,
            "Serviço de teste de integração", [new PecaRequest("Peça teste", 3, 101, 70)], 12000, true, 30);
        using var createOs = await http.PostAsJsonAsync("api/v1/ordens-servico", pedido);
        Check(createOs.StatusCode == HttpStatusCode.Created, "Criação real de OS não retornou 201.");
        var criada = await createOs.Content.ReadFromJsonAsync<CriarOrdemServicoResponse>();
        Check(criada is not null && criada.Id > 0 && criada.TotalCentavos == 12303, "Resposta de criação incorreta.");

        await using (var context = new AppDbContext(options))
        {
            var salva = await context.OrdemServico.AsNoTracking().Include(o => o.Pecas).SingleAsync(o => o.Id == criada!.Id);
            var pecaSalva = salva.Pecas.Single();
            Check(salva.TotalCentavos == 12303 && pecaSalva.ValorUnitarioCentavos == 101 && pecaSalva.CustoUnitarioCentavos == 70,
                "OS e peças persistidas divergem do total ou custo esperado.");
            Check(salva.NomeCliente == "Cliente Teste Integração" && salva.DataConclusaoUtc is null &&
                salva.DataPrevistaRetornoUtc is null && salva.PrazoRetornoDias == 30, "Snapshot ou retorno incorreto.");
        }
        Console.WriteLine("Persistência: POST 201; OS e peça (com custo 70 centavos) gravadas; total 12303 centavos; retorno ainda sem data.");

        using var updateCliente = await http.PutAsJsonAsync("api/v1/clientes", new UpdateClienteDto
        {
            Id = clienteId,
            Nome = "Cliente Teste Alterado",
            Ativo = true
        });
        Check(updateCliente.StatusCode == HttpStatusCode.NoContent, "Falha ao atualizar cliente fictício.");
        await using (var context = new AppDbContext(options))
        {
            var salva = await context.OrdemServico.AsNoTracking().SingleAsync(o => o.Id == criada!.Id);
            Check(salva.NomeCliente == "Cliente Teste Integração", "Alterar cadastro alterou o histórico da OS.");
        }
        Console.WriteLine("Histórico: alterar o nome do cliente não altera o snapshot persistido na OS.");

        var rollback = OrdemServico.Criar(new Cliente { Id = clienteId, Ativo = true }, "AAA0A00", "Marca", "Modelo", "Azul",
            null, "Teste de rollback", [new PecaOrdemServico { Nome = new string('X', 201), Quantidade = 1, ValorUnitarioCentavos = 100, CustoUnitarioCentavos = 70 }],
            0, false, null);
        var falhaEsperada = false;
        var probe = new SqlInsertProbe();
        var rollbackOptions = new DbContextOptionsBuilder<AppDbContext>().UseMySQL(builder.ConnectionString).AddInterceptors(probe).Options;
        await using (var context = new AppDbContext(rollbackOptions))
        {
            try { await new OrdemServicoRepository(context).AddAsync(rollback); }
            catch (DbUpdateException error) when (error.InnerException is MySqlException { Number: 1406 })
            { falhaEsperada = true; }
        }
        Check(falhaEsperada && probe.OsInserida, "Falha de peça não ocorreu após a inserção da OS como esperado.");
        await using (var context = new AppDbContext(options))
        {
            Check(await context.OrdemServico.CountAsync(o => o.ClienteId == clienteId) == 1,
                "Rollback deixou uma segunda OS parcial no banco.");
            Check(await context.PecasOrdemServico.CountAsync(p => p.OrdemServico.ClienteId == clienteId) == 1, "Rollback deixou peças parciais no banco.");
        }
        Console.WriteLine("Rollback real: peça inválida falha após inserir a OS; transaction remove a OS parcial.");
    }
    finally
    {
        if (clienteId > 0)
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM `OrdensServico` WHERE `ClienteId` = {clienteId}");
            await cleanup.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM `Clientes` WHERE `Id` = {clienteId}");
            Console.WriteLine("Dados fictícios desta verificação removidos por seus IDs; nenhum dado preexistente alterado.");
        }
    }
    Console.WriteLine("Verificação real de API e MySQL: OK.");
    return 0;
}
catch (CheckException error)
{
    Console.Error.WriteLine(error.Message);
    return 1;
}
catch (Exception error)
{
    Console.Error.WriteLine($"Verificação não concluída: {error.GetType().Name}. Credenciais não exibidas.");
    return 1;
}

static void Check(bool condition, string message)
{
    if (!condition) throw new CheckException(message);
}
sealed class CheckException(string message) : Exception(message);

sealed class SqlInsertProbe : DbCommandInterceptor
{
    public bool OsInserida { get; private set; }
    private void Observar(DbCommand command)
    {
        if (command.CommandText.Contains("INSERT INTO `OrdensServico`", StringComparison.OrdinalIgnoreCase))
            OsInserida = true;
    }
    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
        DbDataReader result, CancellationToken cancellationToken = default)
    {
        Observar(command);
        return ValueTask.FromResult(result);
    }
    public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
        int result, CancellationToken cancellationToken = default)
    {
        Observar(command);
        return ValueTask.FromResult(result);
    }
}
