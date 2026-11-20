using System.Text.RegularExpressions;
using DeSanti.Application.DTOs.Cliente;

namespace DeSanti.Application.Validators.Clientes;

public static class CriarClienteValidator
{
    public static string SomenteDigitos(string? valor) =>
        string.IsNullOrWhiteSpace(valor)
            ? string.Empty
            : Regex.Replace(valor, @"[^0-9]", string.Empty);

    public static Dictionary<string, string[]> Validar(CreateClienteDto? dto)
    {
        if (dto is null)
            return CamposObrigatorios();

        return ValidarCampos(dto.Nome, dto.Documento, dto.Telefone, dto.Email);
    }

    public static Dictionary<string, string[]> ValidarAtualizacao(UpdateClienteDto? dto)
    {
        if (dto is null)
            return CamposObrigatorios();

        var erros = ValidarCampos(dto.Nome, dto.Documento, dto.Telefone, dto.Email);
        if (dto.Id <= 0)
            erros["id"] = ["Cliente inválido."];

        return erros;
    }

    private static Dictionary<string, string[]> CamposObrigatorios() => new()
    {
        ["nome"] = ["Informe o nome do cliente."],
        ["documento"] = ["Informe um CPF (11 dígitos) ou CNPJ (14 dígitos)."],
        ["telefone"] = ["Informe um telefone válido com DDD."]
    };

    private static Dictionary<string, string[]> ValidarCampos(
        string? nomeBruto, string? documentoBruto, string? telefoneBruto, string? emailBruto)
    {
        var erros = new Dictionary<string, string[]>();

        void Erro(string campo, string mensagem) => erros[campo] = [mensagem];

        var nome = nomeBruto?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(nome))
            Erro("nome", "Informe o nome do cliente.");
        else if (nome.Length > 200)
            Erro("nome", "Use até 200 caracteres.");

        var documento = SomenteDigitos(documentoBruto);
        if (documento.Length is not (11 or 14))
            Erro("documento", "Informe um CPF (11 dígitos) ou CNPJ (14 dígitos).");

        var telefone = SomenteDigitos(telefoneBruto);
        if (telefone.Length is not (10 or 11))
            Erro("telefone", "Informe um telefone válido com DDD.");

        var email = emailBruto?.Trim() ?? string.Empty;
        if (email.Length > 200)
            Erro("email", "Use até 200 caracteres.");

        return erros;
    }
}
