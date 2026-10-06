namespace DeSanti.Domain;

public class Cliente
{
    public long Id { get; set; }
    public string Nome { get; set; }
    public string Email { get; set; }
    public string Telefone { get; set; }
    public string Documento { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime DataAlteracao { get; set; }
    public bool Ativo { get; set; }
}