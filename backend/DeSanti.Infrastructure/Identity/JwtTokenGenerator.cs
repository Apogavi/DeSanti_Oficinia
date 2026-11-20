using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DeSanti.Application.Interfaces.Services;
using DeSanti.Domain;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DeSanti.Infrastructure.Identity;

public sealed class JwtTokenGenerator(IOptions<JwtOptions> options) : IJwtTokenGenerator
{
    private readonly JwtOptions _options = options.Value;

    public (string Token, DateTime ExpiresAtUtc) Generate(Usuario usuario)
    {
        if (string.IsNullOrWhiteSpace(_options.SecretKey) || _options.SecretKey.Length < 32)
            throw new InvalidOperationException("Configuração Jwt:SecretKey ausente ou menor que 32 caracteres.");

        var expiresAtUtc = DateTime.UtcNow.AddHours(Math.Clamp(_options.ExpirationHours, 1, 168));
        var userIdStr = usuario.Id.ToString();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userIdStr),
            new(ClaimTypes.NameIdentifier, userIdStr),
            new(ClaimTypes.Name, usuario.NomeUsuario),
            new("display_name", usuario.Nome),
            new(ClaimTypes.Role, usuario.Role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }
}
