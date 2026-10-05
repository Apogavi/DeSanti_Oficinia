using DeSanti.API.OrdensServico;

namespace DeSanti.Api.Tests;

public class CriarOrdemServicoValidatorTests
{
    [Fact]
    public void AceitaPedidoValido()
    {
        Assert.Empty(CriarOrdemServicoValidator.Validar(PedidoValido()));
    }

    [Fact]
    public void RejeitaNomeVazio()
    {
        var pedido = PedidoValido() with { NomeCliente = " " };

        Assert.Contains(
            "nomeCliente",
            CriarOrdemServicoValidator.Validar(pedido).Keys
        );
    }

    [Fact]
    public void RejeitaCpfInvalido()
    {
        var pedido = PedidoValido() with { Cpf = new string('0', 11) };

        Assert.Contains(
            "cpf",
            CriarOrdemServicoValidator.Validar(pedido).Keys
        );
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

    private static CriarOrdemServicoRequest PedidoValido() => new(
        "Cliente Teste",
        CpfSintetico(),
        new string('0', 11),
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

    private static string CpfSintetico()
    {
        var cpf = "000000001";

        for (var posicao = 9; posicao <= 10; posicao++)
        {
            var soma = 0;

            for (var i = 0; i < posicao; i++)
                soma += (cpf[i] - '0') * (posicao + 1 - i);

            var digito = (soma * 10) % 11;
            cpf += digito == 10 ? '0' : (char)('0' + digito);
        }

        return cpf;
    }
}