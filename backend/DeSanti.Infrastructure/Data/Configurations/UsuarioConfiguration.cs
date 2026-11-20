using DeSanti.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeSanti.Infrastructure.Data.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedOnAdd();

        builder.Property(u => u.NomeUsuario)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(u => u.Nome)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(u => u.Email)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(u => u.Role)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(u => u.Ativo)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(u => u.DataCriacaoUtc).IsRequired();
        builder.Property(u => u.DataAlteracaoUtc).IsRequired(false);

        builder.HasIndex(u => u.NomeUsuario)
            .IsUnique()
            .HasDatabaseName("idx_usuarios_nome_usuario");
    }
}
