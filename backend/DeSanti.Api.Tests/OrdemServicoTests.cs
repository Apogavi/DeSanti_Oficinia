using DeSanti.Domain;

namespace DeSanti.Api.Tests;

public class OrdemServicoTests
{
    [Fact]
    public void CalculaTotalEmCentavosECopiaClienteSemPreverRetorno()
    {
        var cliente = new Cliente
        {
            Id = 1,
            Nome = "Cliente Teste",
            Documento = "00000000000",
            Telefone = "00000000000",
            Ativo = true
        };
        var ordem = OrdemServico.Criar(cliente, "AAA0A00", "Marca Teste", "Modelo Teste", "Azul",
            null, "Serviço de teste",
            [new PecaOrdemServico { Nome = "Peça teste", Quantidade = 3, ValorUnitarioCentavos = 101, CustoUnitarioCentavos = 70 }],
            12000, true, 30);

        cliente.Nome = "Nome alterado";
        cliente.Telefone = "11111111111";

        Assert.Equal(12303, ordem.TotalCentavos);
        Assert.Equal("Cliente Teste", ordem.NomeCliente);
        Assert.Equal("00000000000", ordem.TelefoneCliente);
        Assert.Null(ordem.DataConclusaoUtc);
        Assert.Null(ordem.DataPrevistaRetornoUtc);
        Assert.Equal(DateTimeKind.Utc, ordem.DataCriacaoUtc.Kind);
        // Não associar a navigation impede que Add(os) tente inserir o cliente existente.
        Assert.Null(ordem.Cliente);
    }

    [Theory]
    [InlineData(2, long.MaxValue, 0)]
    [InlineData(1, long.MaxValue, 1)]
    public void RejeitaOverflowNaMultiplicacaoOuSoma(int quantidade, long valor, long maoDeObra)
    {
        Assert.Throws<OverflowException>(() => OrdemServico.CalcularTotalCentavos(
            [new PecaOrdemServico { Quantidade = quantidade, ValorUnitarioCentavos = valor }], maoDeObra));
    }

    [Fact]
    public void RejeitaQuantidadeInvalidaMesmoForaDaApi()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => OrdemServico.CalcularTotalCentavos(
            [new PecaOrdemServico { Quantidade = 0, ValorUnitarioCentavos = 100 }], 0));
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(70L)]
    [InlineData(200L)]
    public void CustoPreservaValorInformadoSemAlterarTotalCobrado(long custo)
    {
        var ordem = OrdemServico.Criar(
            new Cliente { Id = 1, Ativo = true }, "AAA0A00", "Marca", "Modelo", "Cor",
            null, "Serviço", [new PecaOrdemServico
            {
                Nome = "Peça teste", Quantidade = 3,
                ValorUnitarioCentavos = 101, CustoUnitarioCentavos = custo
            }], 12000, false, null);

        Assert.Equal(custo, Assert.Single(ordem.Pecas).CustoUnitarioCentavos);
        Assert.Equal(12303, ordem.TotalCentavos);
    }

    [Fact]
    public void RejeitaPecaSemCustoMesmoForaDaApi()
    {
        Assert.Throws<ArgumentException>(() => OrdemServico.Criar(
            new Cliente { Id = 1, Ativo = true }, "AAA0A00", "Marca", "Modelo", "Cor",
            null, "Serviço", [new PecaOrdemServico
            {
                Nome = "Peça teste", Quantidade = 1,
                ValorUnitarioCentavos = 101, CustoUnitarioCentavos = null
            }], 0, false, null));
    }

    [Fact]
    public void RejeitaCustoNegativoMesmoForaDaApi()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => OrdemServico.Criar(
            new Cliente { Id = 1, Ativo = true }, "AAA0A00", "Marca", "Modelo", "Cor",
            null, "Serviço", [new PecaOrdemServico
            {
                Nome = "Peça teste", Quantidade = 1,
                ValorUnitarioCentavos = 101, CustoUnitarioCentavos = -1
            }], 0, false, null));
    }

    [Fact]
    public void ExigePrazoAoAbrirOsComRetorno()
    {
        Assert.Throws<ArgumentException>(() => OrdemServico.Criar(
            new Cliente { Id = 1, Ativo = true }, "AAA0A00", "Marca", "Modelo", "Cor",
            null, "Serviço", [], 0, true, null));
    }

    [Fact]
    public void FinalizarComRetornoCalculaPrevisaoAPartirDaConclusao()
    {
        var ordem = OrdemServico.Criar(
            new Cliente { Id = 1, Ativo = true }, "AAA0A00", "Marca", "Modelo", "Cor",
            null, "Troca de óleo", [], 10000, true, 90);
        var conclusao = new DateTime(2026, 11, 13, 14, 30, 0, DateTimeKind.Utc);

        ordem.Finalizar(conclusao);

        Assert.Equal(conclusao, ordem.DataConclusaoUtc);
        Assert.Equal(conclusao.AddDays(90), ordem.DataPrevistaRetornoUtc);
    }

    [Fact]
    public void FinalizarSemRetornoMantemPrevisaoNula()
    {
        var ordem = OrdemServico.Criar(
            new Cliente { Id = 1, Ativo = true }, "AAA0A00", "Marca", "Modelo", "Cor",
            null, "Alinhamento", [], 8000, false, null);
        var conclusao = new DateTime(2026, 11, 13, 14, 30, 0, DateTimeKind.Utc);

        ordem.Finalizar(conclusao);

        Assert.Equal(conclusao, ordem.DataConclusaoUtc);
        Assert.Null(ordem.DataPrevistaRetornoUtc);
    }

    [Fact]
    public void RejeitaFinalizarOsJaConcluida()
    {
        var ordem = OrdemServico.Criar(
            new Cliente { Id = 1, Ativo = true }, "AAA0A00", "Marca", "Modelo", "Cor",
            null, "Alinhamento", [], 8000, false, null);
        ordem.Finalizar(new DateTime(2026, 11, 13, 14, 30, 0, DateTimeKind.Utc));

        Assert.Throws<InvalidOperationException>(() =>
            ordem.Finalizar(new DateTime(2026, 11, 14, 10, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public void RejeitaFinalizarComDataUnspecified()
    {
        var ordem = OrdemServico.Criar(
            new Cliente { Id = 1, Ativo = true }, "AAA0A00", "Marca", "Modelo", "Cor",
            null, "Alinhamento", [], 8000, false, null);

        Assert.Throws<ArgumentException>(() =>
            ordem.Finalizar(new DateTime(2026, 11, 13, 14, 30, 0, DateTimeKind.Unspecified)));
    }

    [Fact]
    public void CriarOsSemPecasCalculaTotalApenasComMaoDeObra()
    {
        var ordem = OrdemServico.Criar(
            new Cliente { Id = 1, Ativo = true }, "AAA0A00", "Marca", "Modelo", "Cor",
            2020, "Diagnóstico eletrônico sem troca de peças", [], 18000, true, 15);

        Assert.Empty(ordem.Pecas);
        Assert.Equal(18000, ordem.MaoDeObraCentavos);
        Assert.Equal(18000, ordem.TotalCentavos);
        Assert.True(ordem.NecessitaRetorno);
        Assert.Equal(15, ordem.PrazoRetornoDias);
    }

    [Fact]
    public void CriarOrcamentoAprovarEFinalizarSegueFluxoDeStatus()
    {
        var ordem = OrdemServico.Criar(
            new Cliente { Id = 1, Ativo = true }, "AAA0A00", "Marca", "Modelo", "Cor",
            2021, "Orçamento revisão", [], 12000, false, null, ehOrcamento: true, idempotencyKey: "  chave-123  ");

        Assert.Equal(StatusOrdemServico.Orcamento, ordem.Status);
        Assert.Equal("chave-123", ordem.IdempotencyKey);
        Assert.Throws<InvalidOperationException>(() =>
            ordem.Finalizar(new DateTime(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc)));

        ordem.AprovarOrcamento();
        Assert.Equal(StatusOrdemServico.Aberta, ordem.Status);
        Assert.Throws<InvalidOperationException>(() => ordem.AprovarOrcamento());

        ordem.Finalizar(new DateTime(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc));
        Assert.Equal(StatusOrdemServico.Concluida, ordem.Status);
    }
}
