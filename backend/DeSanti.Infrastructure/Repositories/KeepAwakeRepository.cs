using DeSanti.Application.Interfaces.Repositories;
using DeSanti.Domain;
using DeSanti.Infrastructure.Data;

namespace DeSanti.Infrastructure.Repositories;

public sealed class KeepAwakeRepository(AppDbContext context) : IKeepAwakeRepository
{
    public async Task<KeepAwakeRecord> PingAsync(CancellationToken ct = default)
    {
        var record = new KeepAwakeRecord
        {
            Status = 1,
            UpdatedAtUtc = DateTime.UtcNow
        };

        context.KeepAwakeRecords.Add(record);
        await context.SaveChangesAsync(ct);

        return record;
    }
}
