using DeSanti.Application.DTOs.Cliente;
using DeSanti.Application.Validators.Clientes;

namespace DeSanti.Api.Tests;

public class CriarClienteValidatorTests
{
    [Theory]
    [InlineData("12345678901", "11987654321")]
    [InlineData("123.456.789-01", "(11) 98765-4321")]
    [InlineData("12345678000199", "1134567890")]
    [InlineData("12.345.678/0001-99", "(11) 3456-7890")]
    public void AceitaCpfOuCnpjETelefoneValidos(string documento, string telefone)
    {
        var dto = new CreateClienteDto
        {
            Nome = "Cliente Oficina",
            Documento = documento,
            Telefone = telefone,
            Email = ""
        };

        Assert.Empty(CriarClienteValidator.Validar(dto));
    }

    [Theory]
    [InlineData("", "12345678901", "11987654321", "nome")]
    [InlineData("   ", "12345678901", "11987654321", "nome")]
    [InlineData("Cliente", "", "11987654321", "documento")]
    [InlineData("Cliente", "1234567890", "11987654321", "documento")]
    [InlineData("Cliente", "123456789012", "11987654321", "documento")]
    [InlineData("Cliente", "12345678901", "", "telefone")]
    [InlineData("Cliente", "12345678901", "119876543", "telefone")]
    public void RejeitaCamposObrigatoriosInvalidos(
        string nome, string documento, string telefone, string campoEsperado)
    {
        var dto = new CreateClienteDto
        {
            Nome = nome,
            Documento = documento,
            Telefone = telefone,
            Email = ""
        };

        var erros = CriarClienteValidator.Validar(dto);

        Assert.Contains(campoEsperado, erros.Keys);
    }
}
