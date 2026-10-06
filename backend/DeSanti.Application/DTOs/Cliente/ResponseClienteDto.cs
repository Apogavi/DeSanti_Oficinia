namespace DeSanti.Application.DTOs.Cliente;

public class ResponseClienteDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAlteracao { get; set; }
    public bool Ativo { get; set; }
}