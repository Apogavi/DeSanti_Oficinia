using System.Text.RegularExpressions;
using DeSanti.Application.DTOs.OrdensServico;
using DeSanti.Domain;

namespace DeSanti.Application.Validators.OrdensServico;

public static class CriarOrdemServicoValidator
{
    public static Dictionary<string, string[]> Validar(CriarOrdemServicoRequest pedido)
    {
        var erros = new Dictionary<string, string[]>();

        void Erro(string campo, string mensagem) => erros[campo] = [mensagem];

        if (pedido.ClienteId <= 0)
            Erro("clienteId", "Selecione um cliente.");

        if (!Regex.IsMatch(pedido.Placa ?? "", @"^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$"))
            Erro("placa", "Placa inválida.");

        if (string.IsNullOrWhiteSpace(pedido.Marca))
            Erro("marca", "Informe a marca.");

        if (string.IsNullOrWhiteSpace(pedido.Modelo))
            Erro("modelo", "Informe o modelo.");

        if (string.IsNullOrWhiteSpace(pedido.Cor))
            Erro("cor", "Informe a cor.");

        // Os limites refletem os mappings existentes; evitam truncamento no MySQL.
        if (pedido.Marca?.Length > 100) Erro("marca", "Use até 100 caracteres.");
        if (pedido.Modelo?.Length > 100) Erro("modelo", "Use até 100 caracteres.");
        if (pedido.Cor?.Length > 50) Erro("cor", "Use até 50 caracteres.");

        if (pedido.Ano is int ano && (ano < 1900 || ano > DateTime.Today.Year + 1))
            Erro("ano", "Informe um ano válido.");

        if (string.IsNullOrWhiteSpace(pedido.DescricaoServico))
            Erro("descricaoServico", "Descreva o serviço.");

        if (pedido.MaoDeObraCentavos < 0)
            Erro("maoDeObraCentavos", "O valor não pode ser negativo.");

        if (pedido.Pecas is null)
        {
            Erro("pecas", "Informe a lista de peças.");
        }
        else
        {
            for (var i = 0; i < pedido.Pecas.Count; i++)
            {
                var peca = pedido.Pecas[i];

                if (peca is null || string.IsNullOrWhiteSpace(peca.Nome))
                    Erro($"pecas[{i}].nome", "Informe o nome da peça.");

                if (peca?.Nome?.Length > 200)
                    Erro($"pecas[{i}].nome", "Use até 200 caracteres.");

                if (peca is null || peca.Quantidade <= 0)
                    Erro($"pecas[{i}].quantidade", "A quantidade deve ser maior que zero.");

                if (peca is null || peca.ValorUnitarioCentavos < 0)
                    Erro($"pecas[{i}].valorUnitarioCentavos", "O valor não pode ser negativo.");

                if (peca?.ValorTotalCentavos < 0)
                    Erro($"pecas[{i}].valorTotalCentavos", "O total não pode ser negativo.");
                if (peca is null || peca.CustoUnitarioCentavos is null)
                    Erro($"pecas[{i}].custoUnitarioCentavos", "Informe o custo da peça.");
                else if (peca.CustoUnitarioCentavos < 0)
                    Erro($"pecas[{i}].custoUnitarioCentavos", "O custo da peça não pode ser negativo.");
            }
        }

        if (pedido.NecessitaRetorno && pedido.PrazoRetornoDias is not > 0)
            Erro("prazoRetornoDias", "Informe um prazo maior que zero.");

        if (!pedido.NecessitaRetorno && pedido.PrazoRetornoDias is not null)
            Erro("prazoRetornoDias", "Deixe o prazo vazio quando não houver retorno.");

        if (erros.Count == 0)
        {
            try
            {
                OrdemServico.CalcularTotalCentavos(pedido.Pecas!.Select(p => new PecaOrdemServico
                {
                    Quantidade = p.Quantidade,
                    ValorTotalCentavos = p.ValorTotalCentavos,
                    ValorUnitarioCentavos = p.ValorUnitarioCentavos
                }), pedido.MaoDeObraCentavos);
            }
            catch (OverflowException)
            {
                Erro("totalCentavos", "O total excede o limite de um inteiro de 64 bits.");
            }
        }

        return erros;
    }
}
