using System.Text.RegularExpressions;

namespace DeSanti.API.OrdensServico;

public static class CriarOrdemServicoValidator
{
    public static Dictionary<string, string[]> Validar(CriarOrdemServicoRequest pedido)
    {
        var erros = new Dictionary<string, string[]>();

        void Erro(string campo, string mensagem) => erros[campo] = [mensagem];

        if (string.IsNullOrWhiteSpace(pedido.NomeCliente))
            Erro("nomeCliente", "Informe o nome do cliente.");

        if (!CpfValido(pedido.Cpf))
            Erro("cpf", "CPF inválido.");

        if (!Regex.IsMatch(pedido.Telefone ?? "", @"^\d{11}$"))
            Erro("telefone", "Informe um celular com DDD.");

        if (!Regex.IsMatch(pedido.Placa ?? "", @"^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$"))
            Erro("placa", "Placa inválida.");

        if (string.IsNullOrWhiteSpace(pedido.Marca))
            Erro("marca", "Informe a marca.");

        if (string.IsNullOrWhiteSpace(pedido.Modelo))
            Erro("modelo", "Informe o modelo.");

        if (string.IsNullOrWhiteSpace(pedido.Cor))
            Erro("cor", "Informe a cor.");

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

                if (peca is null || peca.Quantidade <= 0)
                    Erro($"pecas[{i}].quantidade", "A quantidade deve ser maior que zero.");

                if (peca is null || peca.ValorUnitarioCentavos < 0)
                    Erro($"pecas[{i}].valorUnitarioCentavos", "O valor não pode ser negativo.");
            }
        }

        if (pedido.NecessitaRetorno && pedido.PrazoRetornoDias is not > 0)
            Erro("prazoRetornoDias", "Informe um prazo maior que zero.");

        if (!pedido.NecessitaRetorno && pedido.PrazoRetornoDias is not null)
            Erro("prazoRetornoDias", "Deixe o prazo vazio quando não houver retorno.");

        return erros;
    }

    private static bool CpfValido(string? cpf)
    {
        if (cpf is null ||
            !Regex.IsMatch(cpf, @"^\d{11}$") ||
            cpf.Distinct().Count() == 1)
            return false;

        for (var posicao = 9; posicao <= 10; posicao++)
        {
            var soma = 0;

            for (var i = 0; i < posicao; i++)
                soma += (cpf[i] - '0') * (posicao + 1 - i);

            var verificador = (soma * 10) % 11;

            if (verificador == 10) verificador = 0;

            if (verificador != cpf[posicao] - '0')
                return false;
        }

        return true;
    }
}
