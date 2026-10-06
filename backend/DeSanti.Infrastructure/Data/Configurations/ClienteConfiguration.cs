using DeSanti.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeSanti.Infrastructure.Data.Configurations;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .ValueGeneratedOnAdd();

        builder.Property(c => c.Nome)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Documento)
            .HasMaxLength(20)
            .IsRequired(false);

        builder.Property(c => c.Telefone)
            .HasMaxLength(20);

        builder.Property(c => c.Email)
            .HasMaxLength(200);

        builder.Property(c => c.Ativo)
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasIndex(c => c.Nome)
            .HasDatabaseName("idx_clientes_nome");
        
        builder.HasIndex(c => c.Documento)
            .HasDatabaseName("idx_clientes_documento");
    }
}