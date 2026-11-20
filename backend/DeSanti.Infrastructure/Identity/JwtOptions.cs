namespace DeSanti.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "DeSanti.Api";
    public string Audience { get; set; } = "DeSanti.Frontend";
    public int ExpirationHours { get; set; } = 12;
}
