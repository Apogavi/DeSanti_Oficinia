using DeSanti.Domain;

namespace DeSanti.Application.Interfaces.Services;

public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAtUtc) Generate(Usuario usuario);
}
