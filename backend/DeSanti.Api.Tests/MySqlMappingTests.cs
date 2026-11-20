using DeSanti.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeSanti.Api.Tests;

public class MySqlMappingTests
{
    [Fact]
    public void ProviderOficialGeraSqlDoModeloSemConectarAoBanco()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL("Server=127.0.0.1;Database=desanti_exemplo;User ID=usuario_exemplo;Password=senha_exemplo")
            .Options;
        using var context = new AppDbContext(options);
        // Apenas gera SQL em memória: não executa schema, migration ou conexão.
        var sql = context.Database.GenerateCreateScript();
        Assert.Contains("CREATE TABLE `OrdensServico`", sql);
        Assert.Contains("CREATE TABLE `PecasOrdemServico`", sql);
        Assert.Contains("`TotalCentavos` bigint", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("`CustoUnitarioCentavos` bigint NULL", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("`Status` int", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("`IdempotencyKey` varchar(64) NULL", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CREATE TABLE `Usuarios`", sql);
        Assert.Contains("`NomeUsuario` varchar(64)", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ON DELETE RESTRICT", sql);
        Assert.Contains("ON DELETE CASCADE", sql);
    }
}
