namespace DeSanti.Application.DTOs.Cliente;

public class CreateClienteDto
{
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
}