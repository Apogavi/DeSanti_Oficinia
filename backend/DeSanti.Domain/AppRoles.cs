namespace DeSanti.Domain;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Operador = "Operador";

    public static readonly string[] All = [Admin, Operador];

    public static bool IsValid(string? role) =>
        !string.IsNullOrWhiteSpace(role) &&
        All.Contains(role.Trim(), StringComparer.OrdinalIgnoreCase);

    public static string Normalize(string role) =>
        All.First(r => string.Equals(r, role.Trim(), StringComparison.OrdinalIgnoreCase));
}
