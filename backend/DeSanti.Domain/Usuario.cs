namespace DeSanti.Domain;

public class Usuario
{
    public long Id { get; set; }
    public string NomeUsuario { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = AppRoles.Operador;
    public bool Ativo { get; set; } = true;
    public DateTime DataCriacaoUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DataAlteracaoUtc { get; set; }
}
