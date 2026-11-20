namespace DeSanti.Domain;

public class KeepAwakeRecord
{
    public int Id { get; set; }
    public short Status { get; set; } = 1;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
