namespace DeSanti.Application.DTOs.Cliente;

public class UpdateClienteDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public bool Ativo { get; set; }
}