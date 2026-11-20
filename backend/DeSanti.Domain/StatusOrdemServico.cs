namespace DeSanti.Domain;

/// <summary>
/// Ciclo de vida da ordem de serviço.
/// Valor padrão histórico (1 = Aberta) preserva compatibilidade com registros existentes.
/// </summary>
public enum StatusOrdemServico
{
    Orcamento = 0,
    Aberta = 1,
    Concluida = 2
}
