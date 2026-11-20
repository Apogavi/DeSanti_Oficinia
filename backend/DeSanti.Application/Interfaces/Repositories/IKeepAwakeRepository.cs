using DeSanti.Domain;

namespace DeSanti.Application.Interfaces.Repositories;

public interface IKeepAwakeRepository
{
    Task<KeepAwakeRecord> PingAsync(CancellationToken ct = default);
}
