using DeSanti.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeSanti.Infrastructure.Data.Configurations;

public class OrdemServicoConfiguration : IEntityTypeConfiguration<OrdemServico>
{
    public void Configure(EntityTypeBuilder<OrdemServico> builder)
    {
        builder.ToTable("OrdensServico");

        builder.HasKey(os => os.Id);
        builder.Property(os => os.Id).ValueGeneratedOnAdd();

        builder.HasOne(os => os.Cliente)
            .WithMany()
            .HasForeignKey(os => os.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(os => os.NomeCliente).HasMaxLength(200).IsRequired();
        builder.Property(os => os.DocumentoCliente).HasMaxLength(20);
        builder.Property(os => os.TelefoneCliente).HasMaxLength(20);

        builder.Property(os => os.Placa).HasMaxLength(7).IsRequired();
        builder.Property(os => os.Marca).HasMaxLength(100).IsRequired();
        builder.Property(os => os.Modelo).HasMaxLength(100).IsRequired();
        builder.Property(os => os.Cor).HasMaxLength(50).IsRequired();
        builder.Property(os => os.DescricaoServico).HasColumnType("text").IsRequired();

        builder.Property(os => os.Status)
            .HasDefaultValue(StatusOrdemServico.Aberta)
            .IsRequired();
        builder.Property(os => os.IdempotencyKey)
            .HasMaxLength(64)
            .IsRequired(false);

        builder.HasMany(os => os.Pecas)
            .WithOne(peca => peca.OrdemServico)
            .HasForeignKey(peca => peca.OrdemServicoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(os => os.ClienteId);
        builder.HasIndex(os => os.Placa);
        builder.HasIndex(os => os.Status);
        builder.HasIndex(os => os.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("idx_ordens_servico_idempotency_key");
    }
}