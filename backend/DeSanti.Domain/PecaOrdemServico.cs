namespace DeSanti.Domain;

public class PecaOrdemServico
{
    public long? ValorTotalCentavos { get; set; }

    public long Id { get; set; }

    public long OrdemServicoId { get; set; }
    public OrdemServico OrdemServico { get; set; } = null!;

    public string Nome { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    // Preço unitário cobrado do cliente; compõe o total da OS.
    public long ValorUnitarioCentavos { get; set; }

    // Custo unitário pago ao fornecedor nesta OS; zero = custo conhecido igual a zero.
    // Desde 10/11/2026 toda peça nova exige custo (OrdemServico.Criar). O tipo continua
    // nullable apenas para ler registros históricos sem custo: a coluna permanece NULL
    // até Diogo definir o tratamento desses registros. Nunca interpretar null como zero.
    public long? CustoUnitarioCentavos { get; set; }
}
