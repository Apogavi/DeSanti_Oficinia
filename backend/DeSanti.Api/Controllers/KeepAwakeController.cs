using System.Security.Cryptography;
using System.Text;
using DeSanti.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeSanti.API.Controllers;

[ApiController]
[Route("api/v1/keep-awake")]
public sealed class KeepAwakeController(
    KeepAwakeService service,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Ping(CancellationToken ct)
    {
        var configuredKey = configuration["KeepAwake:ApiKey"];
        if (!string.IsNullOrWhiteSpace(configuredKey) && !(User.Identity?.IsAuthenticated ?? false))
        {
            var providedKey = Request.Headers["X-Keep-Awake-Key"].FirstOrDefault()
                ?? Request.Query["key"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(providedKey) ||
                !CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(configuredKey),
                    Encoding.UTF8.GetBytes(providedKey)))
            {
                return Unauthorized(new { message = "Chave de acesso inválida para o endpoint keep-awake." });
            }
        }

        var result = await service.PingAsync(ct);
        return Ok(result);
    }
}
