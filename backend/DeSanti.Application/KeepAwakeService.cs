using DeSanti.Application.DTOs.KeepAwake;
using DeSanti.Application.Interfaces.Repositories;

namespace DeSanti.Application;

public sealed class KeepAwakeService(IKeepAwakeRepository repository)
{
    public async Task<KeepAwakeResponseDto> PingAsync(CancellationToken ct = default)
    {
        var record = await repository.PingAsync(ct);
        return new KeepAwakeResponseDto(record.Id, record.Status, record.UpdatedAtUtc);
    }
}
