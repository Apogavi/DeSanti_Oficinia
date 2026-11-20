using DeSanti.Application.DTOs.OrdensServico;
using DeSanti.Application.Validators.OrdensServico;

namespace DeSanti.Api.Tests;

public class CriarOrdemServicoValidatorTests
{
    [Fact]
    public void RejeitaClienteIdInvalido()
    {
        var pedido = PedidoValido() with { ClienteId = 0};

        Assert.Contains("clienteId", CriarOrdemServicoValidator.Validar(pedido).Keys);
    }

    [Fact]
    public void AceitaPedidoValido()
    {
        Assert.Empty(CriarOrdemServicoValidator.Validar(PedidoValido()));
    }

    [Fact]
    public void ExigePrazoQuandoHaRetorno()
    {
        var pedido = PedidoValido() with { NecessitaRetorno = true };

        Assert.Contains(
            "prazoRetornoDias",
            CriarOrdemServicoValidator.Validar(pedido).Keys
        );
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(70L)]
    public void AceitaPecaComCustoConhecido(long custo)
    {
        var pedido = PedidoValido() with { Pecas = [new PecaRequest("Peça teste", 3, 101, custo)] };

        Assert.Empty(CriarOrdemServicoValidator.Validar(pedido));
    }

    [Fact]
    public void RejeitaPecaSemCusto()
    {
        var pedido = PedidoValido() with { Pecas = [new PecaRequest("Peça teste", 3, 101, null)] };

        var erros = CriarOrdemServicoValidator.Validar(pedido);

        Assert.Equal(["Informe o custo da peça."], erros["pecas[0].custoUnitarioCentavos"]);
    }

    [Fact]
    public void RejeitaPecaComCustoNegativo()
    {
        var pedido = PedidoValido() with { Pecas = [new PecaRequest("Peça teste", 3, 101, -1)] };

        var erros = CriarOrdemServicoValidator.Validar(pedido);

        Assert.Equal(["O custo da peça não pode ser negativo."], erros["pecas[0].custoUnitarioCentavos"]);
    }

    [Fact]
    public void IdentificaApenasAPecaSemCusto()
    {
        var pedido = PedidoValido() with
        {
            Pecas =
            [
                new PecaRequest("Peça com custo", 1, 101, 50),
                new PecaRequest("Peça sem custo", 1, 101, null)
            ]
        };

        var erros = CriarOrdemServicoValidator.Validar(pedido);

        Assert.DoesNotContain("pecas[0].custoUnitarioCentavos", erros.Keys);
        Assert.Contains("pecas[1].custoUnitarioCentavos", erros.Keys);
    }

    private static CriarOrdemServicoRequest PedidoValido() => new(
        1L,
        "AAA0A00",
        "Marca Teste",
        "Modelo Teste",
        "Azul",
        null,
        "Serviço de teste",
        [],
        0,
        false,
        null
    );

}
