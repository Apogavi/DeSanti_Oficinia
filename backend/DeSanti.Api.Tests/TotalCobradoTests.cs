using DeSanti.Domain;
namespace DeSanti.Api.Tests;
public class TotalCobradoTests {
 [Theory]
 [InlineData(4,45000)]
 [InlineData(3,10000)]
 public void TotalNaoMultiplicaPelaQuantidade(int quantidade, long total) {
  Assert.Equal(total, OrdemServico.CalcularTotalCentavos([new PecaOrdemServico {
   Quantidade=quantidade, ValorUnitarioCentavos=0, ValorTotalCentavos=total
  }],0));
 }
 [Fact]
 public void HistoricoMantemCalculoUnitario() {
  Assert.Equal(300, OrdemServico.CalcularTotalCentavos([new PecaOrdemServico {
   Quantidade=3, ValorUnitarioCentavos=100
  }],0));
 }
}
