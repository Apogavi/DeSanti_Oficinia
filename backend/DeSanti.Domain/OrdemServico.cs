namespace DeSanti.Domain;

public class OrdemServico
{
    public long Id { get; set; }

    public long ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;

    // Cópia dos dados do cliente no momento da abertura da OS.
    public string NomeCliente { get; set; } = string.Empty;
    public string DocumentoCliente { get; set; } = string.Empty;
    public string TelefoneCliente { get; set; } = string.Empty;

    public string Placa { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public string Cor { get; set; } = string.Empty;
    public int? Ano { get; set; }

    public string DescricaoServico { get; set; } = string.Empty;
    public List<PecaOrdemServico> Pecas { get; set; } = [];

    public long MaoDeObraCentavos { get; set; }
    public long TotalCentavos { get; private set; }

    public bool NecessitaRetorno { get; set; }
    public int? PrazoRetornoDias { get; set; }

    public DateTime DataCriacaoUtc { get; set; }
    public DateTime? DataConclusaoUtc { get; set; }
    public DateTime? DataPrevistaRetornoUtc { get; set; }

    public StatusOrdemServico Status { get; set; } = StatusOrdemServico.Aberta;
    public string? IdempotencyKey { get; set; }

    /// <summary>
    /// Abre a OS (ou orçamento) com os dados históricos do cliente e o total oficial em centavos.
    /// Conclusão e previsão de retorno permanecem vazias até a finalização.
    /// </summary>
    public static OrdemServico Criar(
        Cliente cliente, string placa, string marca, string modelo, string cor, int? ano,
        string descricaoServico, List<PecaOrdemServico> pecas, long maoDeObraCentavos,
        bool necessitaRetorno, int? prazoRetornoDias,
        bool ehOrcamento = false, string? idempotencyKey = null)
    {
        if (cliente.Id <= 0 || !cliente.Ativo)
            throw new ArgumentException("A OS exige um cliente cadastrado e ativo.", nameof(cliente));
        if (necessitaRetorno ? prazoRetornoDias is not > 0 : prazoRetornoDias is not null)
            throw new ArgumentException("Prazo de retorno incompatível com a OS.", nameof(prazoRetornoDias));

        var chaveLimpa = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim();
        if (chaveLimpa is not null && chaveLimpa.Length > 64)
            throw new ArgumentException("A chave de idempotência deve ter até 64 caracteres.", nameof(idempotencyKey));

        // Custo é independente do preço cobrado, mas obrigatório em toda peça nova (10/11/2026).
        foreach (var peca in pecas)
        {
            if (peca.CustoUnitarioCentavos is null)
                throw new ArgumentException("Toda peça deve ter custo conhecido.", nameof(pecas));
            if (peca.CustoUnitarioCentavos < 0)
                throw new ArgumentOutOfRangeException(nameof(pecas), "O custo da peça não pode ser negativo.");
        }

        return new OrdemServico
        {
            ClienteId = cliente.Id,
            NomeCliente = cliente.Nome,
            DocumentoCliente = cliente.Documento ?? string.Empty,
            TelefoneCliente = cliente.Telefone,
            Placa = placa,
            Marca = marca,
            Modelo = modelo,
            Cor = cor,
            Ano = ano,
            DescricaoServico = descricaoServico,
            Pecas = pecas,
            MaoDeObraCentavos = maoDeObraCentavos,
            TotalCentavos = CalcularTotalCentavos(pecas, maoDeObraCentavos),
            NecessitaRetorno = necessitaRetorno,
            PrazoRetornoDias = prazoRetornoDias,
            DataCriacaoUtc = DateTime.UtcNow,
            Status = ehOrcamento ? StatusOrdemServico.Orcamento : StatusOrdemServico.Aberta,
            IdempotencyKey = chaveLimpa
        };
    }

    public void AprovarOrcamento()
    {
        if (Status != StatusOrdemServico.Orcamento)
            throw new InvalidOperationException("Apenas orçamentos podem ser aprovados.");

        Status = StatusOrdemServico.Aberta;
    }

    public void Finalizar(DateTime dataConclusaoUtc)
    {
        if (Status == StatusOrdemServico.Orcamento)
            throw new InvalidOperationException("Aprove o orçamento antes de finalizar a ordem de serviço.");
        if (DataConclusaoUtc.HasValue || Status == StatusOrdemServico.Concluida)
            throw new InvalidOperationException("A ordem de serviço já foi finalizada.");

        var conclusaoUtc = dataConclusaoUtc.Kind switch
        {
            DateTimeKind.Utc => dataConclusaoUtc,
            DateTimeKind.Local => dataConclusaoUtc.ToUniversalTime(),
            _ => throw new ArgumentException(
                "A data de conclusão deve especificar o fuso horário (UTC).", nameof(dataConclusaoUtc))
        };

        Status = StatusOrdemServico.Concluida;
        DataConclusaoUtc = conclusaoUtc;
        DataPrevistaRetornoUtc = NecessitaRetorno && PrazoRetornoDias is > 0
            ? conclusaoUtc.AddDays(PrazoRetornoDias.Value)
            : null;
    }

    /// <summary>Usa checked para rejeitar overflow em multiplicações e somas de Int64.</summary>
    public static long CalcularTotalCentavos(IEnumerable<PecaOrdemServico> pecas, long maoDeObraCentavos)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maoDeObraCentavos);
        var total = maoDeObraCentavos;
        foreach (var peca in pecas)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(peca.Quantidade);
            ArgumentOutOfRangeException.ThrowIfNegative(peca.ValorUnitarioCentavos);
            if (peca.ValorTotalCentavos < 0) throw new ArgumentOutOfRangeException(nameof(pecas));
            total = checked(total + (peca.ValorTotalCentavos ?? checked(peca.Quantidade * peca.ValorUnitarioCentavos)));
        }
        return total;
    }
}
