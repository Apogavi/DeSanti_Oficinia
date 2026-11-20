using DeSanti.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeSanti.Infrastructure.Data.Configurations;

public class KeepAwakeRecordConfiguration : IEntityTypeConfiguration<KeepAwakeRecord>
{
    public void Configure(EntityTypeBuilder<KeepAwakeRecord> builder)
    {
        builder.ToTable("KeepAwakeRecords");

        builder.HasKey(k => k.Id);
        builder.Property(k => k.Id).ValueGeneratedOnAdd();
        builder.Property(k => k.Status).IsRequired();
        builder.Property(k => k.UpdatedAtUtc).IsRequired();
    }
}
